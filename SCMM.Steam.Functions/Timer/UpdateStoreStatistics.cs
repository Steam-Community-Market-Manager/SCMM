using CommandQuery;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Store;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System.Text.RegularExpressions;

namespace SCMM.Steam.Functions.Timer;

public class UpdateStoreStatistics
{
    private readonly SteamConfiguration _steamConfiguration;
    private readonly SteamDbContext _db;
    private readonly ICommandProcessor _commandProcessor;
    private readonly SteamCommunityWebClient _steamCommunityWebClient;

    public UpdateStoreStatistics(SteamConfiguration steamConfiguration, ICommandProcessor commandProcessor, SteamDbContext db, SteamCommunityWebClient steamCommunityWebClient)
    {
        _steamConfiguration = steamConfiguration;
        _commandProcessor = commandProcessor;
        _db = db;
        _steamCommunityWebClient = steamCommunityWebClient;
    }

    [Function("Update-Store-Statistics")]
    public async Task Run([TimerTrigger("0 0/5 * * * *")] /* every 5 minutes */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Store-Statistics");

        var appItemStores = await _db.SteamItemStores
            .Where(x => x.Start == x.App.ItemStores.Max(x => x.Start))
            .Include(x => x.App)
            .Include(x => x.Items).ThenInclude(x => x.Item)
            .Include(x => x.Items).ThenInclude(x => x.Item.Description)
            .ToListAsync();

        foreach (var appItemStore in appItemStores)
        {
            await UpdateItemStoreSubscribers(logger, appItemStore);
            await UpdateItemStoreTopSellers(logger, appItemStore);
        }

        await _db.SaveChangesAsync();
    }

    private async Task UpdateItemStoreTopSellers(ILogger logger, SteamItemStore itemStore)
    {
        logger.LogTrace($"Updating item store top seller statistics (app: {itemStore.App.SteamId})");
        var storePage = await _steamCommunityWebClient.GetStorePage(new SteamItemStorePageRequest()
        {
            AppId = itemStore.App.SteamId,
            Start = 0,
            Count = SteamItemStorePageRequest.MaxPageSize,
            Filter = SteamItemStorePageRequest.FilterFeatured
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

        var items = await _db.SteamStoreItems
            .Where(x => storeItemIds.Contains(x.SteamId) || x.Description.StoreItemTopSellerPositions.Any(y => y.IsActive))
            .Select(x => new
            {
                StoreItemId = x.SteamId,
                DescriptionId = x.DescriptionId,
                LastPosition = _db.SteamStoreItemTopSellerPositions.FirstOrDefault(
                    y => x.DescriptionId == y.DescriptionId && x.Description.StoreItemTopSellerPositions.Max(z => z.Timestamp) == y.Timestamp
                )
            })
            .ToListAsync();

        var total = storeItemIds.Count;
        foreach (var item in items)
        {
            var position = (storeItemIds.Contains(item.StoreItemId) ? (storeItemIds.IndexOf(item.StoreItemId) + 1) : 0);
            if (position > 0)
            {
                if (item.LastPosition != null)
                {
                    item.LastPosition.IsActive = true;
                    // TODO: System.OverflowException: SqlDbType.Time overflow. Value '1.04:35:01.9954923' is out of range. Must be between 00:00:00.0000000 and 23:59:59.9999999. 
                    //item.LastPosition.Duration = (DateTimeOffset.UtcNow - item.LastPosition.Timestamp);
                }
                if (item.LastPosition == null || item.LastPosition.Position != position || item.LastPosition.Total != total)
                {
                    _db.SteamStoreItemTopSellerPositions.Add(
                        new SteamStoreItemTopSellerPosition()
                        {
                            Timestamp = DateTimeOffset.UtcNow,
                            DescriptionId = item.DescriptionId,
                            Position = position,
                            Total = total,
                            IsActive = true
                        }
                    );
                }
            }
            else
            {
                if (item.LastPosition != null)
                {
                    item.LastPosition.IsActive = false;
                }
            }
        }

        await _db.SaveChangesAsync();
    }

    private async Task UpdateItemStoreSubscribers(ILogger logger, SteamItemStore itemStore)
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

        logger.LogTrace($"Updating item store workshop statistics (ids: {workshopFileIds.Count})");
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
            _ = await _commandProcessor.ProcessAsync(new UpdateSteamAssetDescriptionRequest()
            {
                AssetDescription = item.AssetDescription,
                PublishedFile = item.PublishedFile
            });
        }

        await _db.SaveChangesAsync();
    }
}
