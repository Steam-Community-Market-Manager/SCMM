using CommandQuery;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Store.Requests.Html;
using SCMM.Steam.Data.Models.WebApi.Requests.ISteamRemoteStorage;
using SCMM.Steam.Data.Store;
using System.Text.RegularExpressions;

namespace SCMM.Steam.Functions.Timer;

public class UpdateStoreStatistics
{
    private readonly SteamConfiguration _steamConfiguration;
    private readonly SteamDbContext _steamDb;
    private readonly SteamWebApiClient _steamApiClient;
    private readonly ICommandProcessor _commandProcessor;
    private readonly SteamStoreWebClient _steamStoreWebClient;

    public UpdateStoreStatistics(SteamConfiguration steamConfiguration, ICommandProcessor commandProcessor, SteamDbContext steamDb, SteamWebApiClient steamApiClient, SteamStoreWebClient steamStoreWebClient)
    {
        _steamConfiguration = steamConfiguration;
        _commandProcessor = commandProcessor;
        _steamDb = steamDb;
        _steamApiClient = steamApiClient;
        _steamStoreWebClient = steamStoreWebClient;
    }

    [Function("Update-Store-Statistics")]
    public async Task Run([TimerTrigger("0 0/5 * * * *")] /* every 5 minutes */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Store-Statistics");

        var appItemStores = await _steamDb.SteamItemStores
            .Where(x => x.Start != null && x.End == null)
            .Include(x => x.App)
            .Include(x => x.Items).ThenInclude(x => x.Item)
            .Include(x => x.Items).ThenInclude(x => x.Item.Description)
            .ToListAsync();

        foreach (var appItemStore in appItemStores)
        {
            await UpdateItemStoreSubscribers(logger, appItemStore);
            await UpdateItemStoreTopSellers(logger, appItemStore);
        }

        await _steamDb.SaveChangesAsync();
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
            .ToArray();

        if (!workshopFileIds.Any())
        {
            return;
        }

        logger.LogTrace($"Updating item store workshop statistics (ids: {workshopFileIds.Length})");
        var response = await _steamApiClient.SteamRemoteStorageGetPublishedFileDetailsAsync(new GetPublishedFileDetailsJsonRequest()
        {
            PublishedFileIds = workshopFileIds
        });
        if (response?.PublishedFileDetails?.Any() != true)
        {
            logger.LogError("Failed to get published file details");
            return;
        }

        var assetWorkshopJoined = response.PublishedFileDetails.Join(assetDescriptions,
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
            _ = await _commandProcessor.ProcessWithResultAsync(new UpdateSteamAssetDescriptionRequest()
            {
                AssetDescription = item.AssetDescription,
                PublishedFile = item.PublishedFile
            });
        }

        await _steamDb.SaveChangesAsync();
    }

    private async Task UpdateItemStoreTopSellers(ILogger logger, SteamItemStore itemStore)
    {
        logger.LogTrace($"Updating item store top seller statistics (app: {itemStore.App.SteamId})");
        var storePage = await _steamStoreWebClient.GetStorePageAsync(new SteamItemStorePageRequest()
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

        var items = await _steamDb.SteamStoreItems
            .Where(x => storeItemIds.Contains(x.SteamId) || x.Description.StoreItemTopSellerPositions.Any(y => y.IsActive))
            .Select(x => new
            {
                StoreItemId = x.SteamId,
                DescriptionId = x.DescriptionId,
                LastPosition = _steamDb.SteamStoreItemTopSellerPositions.FirstOrDefault(
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
                    _steamDb.SteamStoreItemTopSellerPositions.Add(
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

        await _steamDb.SaveChangesAsync();
    }
}
