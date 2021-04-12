using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Data.Models.Steam;
using SCMM.Web.Server.Extensions;
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
                var db = scope.ServiceProvider.GetRequiredService<ScmmDbContext>();

                var appItemStores = db.SteamItemStores
                    .Include(x => x.App)
                    .Include(x => x.Items).ThenInclude(x => x.Item)
                    .Include(x => x.Items).ThenInclude(x => x.Item.Stores)
                    .Include(x => x.Items).ThenInclude(x => x.Item.Description)
                    .Include(x => x.Items).ThenInclude(x => x.Item.Description.WorkshopFile)
                    .Where(x => x.Start == x.App.ItemStores.Max(x => x.Start))
                    .ToList();

                foreach (var appItemStore in appItemStores)
                {
                    await UpdateItemStoreSubscribers(db, service, appItemStore);
                    await UpdateItemStoreTopSellers(db, commnityClient, service, appItemStore);
                }

                db.SaveChanges();
            }
        }

        private async Task UpdateItemStoreTopSellers(ScmmDbContext db, SteamCommunityClient commnityClient, SteamService service, SteamItemStore itemStore)
        {
            _logger.LogInformation($"Updating item store top seller statistics (app: {itemStore.App.SteamId})");
            var storePage = await commnityClient.GetItemStorePage(new SteamItemStorePageRequest()
            {
                AppId = itemStore.App.SteamId,
                Start = 0,
                Count = SteamItemStorePageRequest.MaxPageSize,
                Filter = SteamItemStorePageRequest.FilterFeatured
            });
            if (storePage == null)
            {
                _logger.LogError("Failed to get item store details");
                return;
            }

            var storeItemIds = new List<string>();
            var storeItemDefs = storePage.Descendants()
                .Where(x => x.Attribute("class")?.Value?.Contains(Constants.SteamStoreItemDef) == true);
            foreach (var storeItemDef in storeItemDefs)
            {
                var storeItemName = storeItemDef.Descendants()
                    .FirstOrDefault(x => x.Attribute("class")?.Value?.Contains(Constants.SteamStoreItemDefName) == true);
                if (storeItemName != null)
                {
                    var storeItemLink = storeItemName.Descendants()
                        .Where(x => x.Name.LocalName == "a")
                        .Select(x => x.Attribute("href"))
                        .FirstOrDefault();
                    if (!String.IsNullOrEmpty(storeItemLink?.Value))
                    {
                        var storeItemIdMatchGroup = Regex.Match(storeItemLink.Value, Constants.SteamStoreItemDefLinkRegex).Groups;
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

            // The "top sellers" list only shows the top 9 store items, this ensures all items are accounted for
            var storeItems = itemStore.Items.ToArray();
            var missingStoreItems = storeItems
                .Where(x => !storeItemIds.Contains(x.Item.SteamId))
                .OrderByDescending(x => x.Item.Description?.WorkshopFile?.Subscriptions ?? 0);
            foreach (var storeItem in missingStoreItems)
            {
                storeItemIds.Add(storeItem.Item.SteamId);
            }

            // Update the store item indecies
            foreach (var storeItem in storeItems)
            {
                service.UpdateStoreItemIndex(storeItem, storeItemIds.IndexOf(storeItem.Item.SteamId));
            }

            // Calculate total sales
            var orderedStoreItems = storeItems.OrderBy(x => x.Index).ToList();
            foreach (var storeItem in orderedStoreItems)
            {
                storeItem.Item.RecalculateTotalSales(itemStore);
            }

            db.SaveChanges();
        }

        private async Task UpdateItemStoreSubscribers(ScmmDbContext db, SteamService service, SteamItemStore itemStore)
        {
            var assetDescriptions = itemStore.Items
                .Select(x => x.Item)
                .Where(x => x.Description?.WorkshopFile?.SteamId != null)
                .Select(x => x.Description)
                .Take(Constants.SteamStoreItemsMax)
                .ToList();

            var workshopFileIds = assetDescriptions
                .Select(x => UInt64.Parse(x.WorkshopFile.SteamId))
                .ToList();

            if (!workshopFileIds.Any())
            {
                return;
            }

            _logger.LogInformation($"Updating item store workshop statistics (ids: {workshopFileIds.Count})");
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

            db.SaveChanges();
        }
    }
}
