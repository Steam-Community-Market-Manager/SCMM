using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Steam.Shared;
using SCMM.Steam.Shared.Community.Requests.Html;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain;
using SCMM.Web.Server.Services.Jobs.CronJob;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs
{
    public class UpdateStoreSalesStatisticsJob : CronJobService
    {
        private readonly ILogger<UpdateStoreSalesStatisticsJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SteamConfiguration _steamConfiguration;

        public UpdateStoreSalesStatisticsJob(IConfiguration configuration, ILogger<UpdateStoreSalesStatisticsJob> logger, IServiceScopeFactory scopeFactory)
            : base(logger, configuration.GetJobConfiguration<UpdateStoreSalesStatisticsJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _steamConfiguration = configuration.GetSteamConfiguration();
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var commnityClient = scope.ServiceProvider.GetService<SteamCommunityClient>();
                var service = scope.ServiceProvider.GetRequiredService<SteamService>();
                var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();

                await UpdateStoreSubscribers(db, service);
                await UpdateStoreTopSellers(db, commnityClient, service);
            }
        }

        private async Task UpdateStoreTopSellers(SteamDbContext db, SteamCommunityClient commnityClient, SteamService service)
        {
            var appStoreItems = db.SteamStoreItems
                .Include(x => x.App)
                .Include(x => x.Description.WorkshopFile)
                .Where(x => x.Description.WorkshopFile.AcceptedOn == x.App.StoreItems.Max(x => x.Description.WorkshopFile.AcceptedOn))
                .Take(SteamConstants.SteamStoreItemsMax)
                .ToList();

            var appStores = appStoreItems.GroupBy(x => x.App.SteamId).ToList();
            foreach (var appStore in appStores)
            {
                _logger.LogInformation($"Updating store item top seller statistics (app: {appStore.Key})");
                var storePage = await commnityClient.GetItemStorePage(new SteamItemStorePageRequest()
                {
                    AppId = appStore.Key,
                    Start = 0,
                    Count = SteamItemStorePageRequest.MaxPageSize,
                    Filter = SteamItemStorePageRequest.FilterFeatured
                });
                if (storePage == null)
                {
                    _logger.LogError("Failed to get item store details");
                    continue;
                }

                var storeItemIds = new List<string>();
                var storeItemDefs = storePage.Descendants()
                    .Where(x => x.Attribute("class")?.Value?.Contains(SteamConstants.SteamStoreItemDef) == true);
                foreach (var storeItemDef in storeItemDefs)
                {
                    var storeItemName = storeItemDef.Descendants()
                        .FirstOrDefault(x => x.Attribute("class")?.Value?.Contains(SteamConstants.SteamStoreItemDefName) == true);
                    if (storeItemName != null)
                    {
                        var storeItemLink = storeItemName.Descendants()
                            .Where(x => x.Name.LocalName == "a")
                            .Select(x => x.Attribute("href"))
                            .FirstOrDefault();
                        if (!String.IsNullOrEmpty(storeItemLink?.Value))
                        {
                            var storeItemIdMatchGroup = Regex.Match(storeItemLink.Value, SteamConstants.SteamStoreItemDefLinkRegex).Groups;
                            var storeItemId = (storeItemIdMatchGroup.Count > 1)
                                ? storeItemIdMatchGroup[1].Value.Trim()
                                : null;
                            if (!String.IsNullOrEmpty(storeItemId))
                            {
                                storeItemIds.Add(storeItemId);
                            }
                        }
                    }
                }

                // Top sellers only shows the top 9 store items, this ensures all items are accounted for
                var storeItems = appStore.ToArray();
                var missingStoreItems = storeItems.Where(x => storeItemIds.Contains(x.SteamId);
                foreach (var storeItem in missingStoreItems)
                {
                    storeItemIds.Add(storeItem.SteamId);
                }

                // Update the store position for all the items
                foreach (var storeItemId in storeItemIds)
                {
                    var storeItem = storeItems.FirstOrDefault(x => x.SteamId == storeItemId);
                    if (storeItem == null)
                    {
                        continue;
                    }

                    var storeRankPosition = (storeItemIds.IndexOf(storeItemId) + 1);
                    var storeRankTotal = storeItemIds.Count;
                    storeItem = service.UpdateStoreItemRank(storeItem, storeRankPosition, storeRankTotal);
                }

                // Calculate total sales twice, once in both directions (to get correct min/max values)
                foreach (var storeItem in storeItems.OrderByDescending(x => x.StoreRankPosition))
                {
                    storeItem.RecalculateTotalSales(storeItems);
                }
                foreach (var storeItem in storeItems.OrderBy(x => x.StoreRankPosition))
                {
                    storeItem.RecalculateTotalSales(storeItems);
                }

                await db.SaveChangesAsync();
            }
        }

        private async Task UpdateStoreSubscribers(SteamDbContext db, SteamService service)
        {
            var assetDescriptions = db.SteamStoreItems
                .Where(x => x.Description.WorkshopFile.SteamId != null)
                .Where(x => x.Description.WorkshopFile.AcceptedOn == x.App.StoreItems.Max(x => x.Description.WorkshopFile.AcceptedOn))
                .Include(x => x.Description.WorkshopFile)
                .Select(x => x.Description)
                .Take(SteamConstants.SteamStoreItemsMax)
                .ToList();

            var workshopFileIds = assetDescriptions
                .Select(x => UInt64.Parse(x.WorkshopFile.SteamId))
                .ToList();

            _logger.LogInformation($"Updating store item workshop statistics (ids: {workshopFileIds.Count})");
            var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_steamConfiguration.ApplicationKey);
            var steamRemoteStorage = steamWebInterfaceFactory.CreateSteamWebInterface<SteamRemoteStorage>();
            var response = await steamRemoteStorage.GetPublishedFileDetailsAsync(workshopFileIds);
            if (response?.Data?.Any() != true)
            {
                _logger.LogError("Failed to get published file details");
                return;
            }

            var assetWorkshopJoined = response.Data.Join(assetDescriptions,
                x => x.PublishedFileId.ToString(),
                y => y.WorkshopFile.SteamId,
                (x, y) => new
                {
                    AssetDescription = y,
                    PublishedFile = x
                }
            );

            foreach (var item in assetWorkshopJoined)
            {
                await service.UpdateAssetDescription(
                    item.AssetDescription, item.PublishedFile, updateSubscriptionGraph: true
                );
            }

            await db.SaveChangesAsync();
        }
    }
}
