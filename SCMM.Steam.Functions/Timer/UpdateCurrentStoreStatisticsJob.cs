using CommandQuery;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Steam.API;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Store;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System.Text.RegularExpressions;

namespace SCMM.Steam.Functions.Timer;

public class UpdateCurrentStoreStatisticsJob
{
    private readonly SteamConfiguration _steamConfiguration;
    private readonly SteamDbContext _db;
    private readonly ICommandProcessor _commandProcessor;
    private readonly SteamCommunityWebClient _steamCommunityWebClient;
    private readonly SteamService _steamService;

    public UpdateCurrentStoreStatisticsJob(SteamConfiguration steamConfiguration, ICommandProcessor commandProcessor, SteamDbContext db, SteamCommunityWebClient steamCommunityWebClient, SteamService steamService)
    {
        _steamConfiguration = steamConfiguration;
        _commandProcessor = commandProcessor;
        _db = db;
        _steamCommunityWebClient = steamCommunityWebClient;
        _steamService = steamService;
    }

    [Function("Update-Store-Statistics")]
    public async Task Run([TimerTrigger("0 0/5 * * * *")] /* every 5 minutes */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Store-Statistics");

        var appItemStores = _db.SteamItemStores
            .Where(x => x.Start == x.App.ItemStores.Max(x => x.Start))
            .Include(x => x.App)
            .Include(x => x.Items).ThenInclude(x => x.Item)
            .Include(x => x.Items).ThenInclude(x => x.Item.Stores)
            .Include(x => x.Items).ThenInclude(x => x.Item.Description)
            .ToList();

        foreach (var appItemStore in appItemStores)
        {
            await UpdateItemStoreSubscribers(logger, _db, _steamService, _commandProcessor, appItemStore);
            await UpdateItemStoreTopSellers(logger, _db, _steamCommunityWebClient, _steamService, appItemStore);
        }

        _db.SaveChanges();
    }

    private async Task UpdateItemStoreTopSellers(ILogger logger, SteamDbContext db, SteamCommunityWebClient commnityClient, SteamService service, SteamItemStore itemStore)
    {
        logger.LogInformation($"Updating item store top seller statistics (app: {itemStore.App.SteamId})");
        var storePage = await commnityClient.GetStorePage(new SteamStorePageRequest()
        {
            AppId = itemStore.App.SteamId,
            Start = 0,
            Count = SteamStorePageRequest.MaxPageSize,
            Filter = SteamStorePageRequest.FilterFeatured
        });
        if (storePage == null)
        {
            logger.LogError("Failed to get item store details");
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
                if (!string.IsNullOrEmpty(storeItemLink?.Value))
                {
                    var storeItemIdMatchGroup = Regex.Match(storeItemLink.Value, Constants.SteamStoreItemDefLinkRegex).Groups;
                    var storeItemId = storeItemIdMatchGroup.Count > 1
                        ? storeItemIdMatchGroup[1].Value.Trim()
                        : null;
                    if (!string.IsNullOrEmpty(storeItemId))
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

    private async Task UpdateItemStoreSubscribers(ILogger logger, SteamDbContext db, SteamService service, ICommandProcessor commandProcessor, SteamItemStore itemStore)
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

        logger.LogInformation($"Updating item store workshop statistics (ids: {workshopFileIds.Count})");
        var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_steamConfiguration.ApplicationKey);
        var steamRemoteStorage = steamWebInterfaceFactory.CreateSteamWebInterface<SteamRemoteStorage>();
        var response = await steamRemoteStorage.GetPublishedFileDetailsAsync(workshopFileIds);
        if (response?.Data?.Any() != true)
        {
            logger.LogError("Failed to get published file details");
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
            _ = await commandProcessor.ProcessWithResultAsync(new UpdateSteamAssetDescriptionRequest()
            {
                AssetDescription = item.AssetDescription,
                PublishedFile = item.PublishedFile
            });
        }

        db.SaveChanges();
    }
}
