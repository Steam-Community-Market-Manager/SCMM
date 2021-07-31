using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.API;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Job.Server.Jobs.Cron;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Steam.Job.Server.Jobs
{
    public class UpdateCurrentStoreStatisticsJob : CronJobService
    {
        private readonly ILogger<UpdateCurrentStoreStatisticsJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SteamConfiguration _steamConfiguration;

        public UpdateCurrentStoreStatisticsJob(IConfiguration configuration, ILogger<UpdateCurrentStoreStatisticsJob> logger, IServiceScopeFactory scopeFactory)
            : base(logger, configuration.GetJobConfiguration<UpdateCurrentStoreStatisticsJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _steamConfiguration = configuration.GetSteamConfiguration();
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var commnityClient = scope.ServiceProvider.GetService<SteamCommunityWebClient>();
            var commandProcessor = scope.ServiceProvider.GetService<ICommandProcessor>();
            var service = scope.ServiceProvider.GetRequiredService<SteamService>();
            var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();

            var appItemStores = db.SteamItemStores
                .Where(x => x.Start == x.App.ItemStores.Max(x => x.Start))
                .Include(x => x.App)
                .Include(x => x.Items).ThenInclude(x => x.Item)
                .Include(x => x.Items).ThenInclude(x => x.Item.Stores)
                .Include(x => x.Items).ThenInclude(x => x.Item.Description)
                .ToList();

            foreach (var appItemStore in appItemStores)
            {
                await UpdateItemStoreSubscribers(db, service, commandProcessor, appItemStore);
                await UpdateItemStoreTopSellers(db, commnityClient, service, appItemStore);
            }

            db.SaveChanges();
        }

        private async Task UpdateItemStoreTopSellers(SteamDbContext db, SteamCommunityWebClient commnityClient, SteamService service, SteamItemStore itemStore)
        {
            _logger.LogInformation($"Updating item store top seller statistics (app: {itemStore.App.SteamId})");
            var storePage = await commnityClient.GetStorePage(new SteamStorePageRequest()
            {
                AppId = itemStore.App.SteamId,
                Start = 0,
                Count = SteamStorePageRequest.MaxPageSize,
                Filter = SteamStorePageRequest.FilterFeatured
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
                .OrderByDescending(x => x.Item.Description?.LifetimeSubscriptions ?? 0);
            foreach (var storeItem in missingStoreItems)
            {
                storeItemIds.Add(storeItem.Item.SteamId);
            }

            // Update the store item indecies
            foreach (var storeItem in storeItems)
            {
                storeItem.TopSellerIndex = storeItemIds.IndexOf(storeItem.Item.SteamId);
            }

            // Calculate total sales
            var orderedStoreItems = storeItems.OrderBy(x => x.TopSellerIndex).ToList();
            foreach (var storeItem in orderedStoreItems)
            {
                storeItem.Item.RecalculateTotalSales(itemStore);
            }

            db.SaveChanges();
        }

        private async Task UpdateItemStoreSubscribers(SteamDbContext db, SteamService service, ICommandProcessor commandProcessor, SteamItemStore itemStore)
        {
            var assetDescriptions = itemStore.Items
                .Select(x => x.Item)
                .Where(x => x.Description?.WorkshopFileId != null)
                .Select(x => x.Description)
                .Take(Constants.SteamStoreItemsMax)
                .ToList();

            var workshopFileIds = assetDescriptions
                .Select(x => x.WorkshopFileId.Value)
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
                x => x.PublishedFileId,
                y => y.WorkshopFileId,
                (x, y) => new
                {
                    AssetDescription = y,
                    PublishedFile = x
                }
            );

            foreach (var item in assetWorkshopJoined)
            {
                _ = await commandProcessor.ProcessAsync(new UpdateSteamAssetDescriptionRequest()
                {
                    AssetDescription = item.AssetDescription,
                    PublishedFile = item.PublishedFile
                });
            }

            db.SaveChanges();
        }
    }
}
