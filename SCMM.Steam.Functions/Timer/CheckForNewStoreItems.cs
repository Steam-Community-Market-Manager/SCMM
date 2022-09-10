using CommandQuery;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Azure.ServiceBus;
using SCMM.Shared.API.Messages;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.API;
using SCMM.Steam.API.Commands;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;
using Steam.Models.SteamEconomy;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;

namespace SCMM.Steam.Functions.Timer;

public class CheckForNewStoreItems
{
    private readonly IConfiguration _configuration;
    private readonly SteamDbContext _steamDb;
    private readonly ICommandProcessor _commandProcessor;
    private readonly IQueryProcessor _queryProcessor;
    private readonly SteamService _steamService;
    private readonly ServiceBusClient _serviceBus;

    public CheckForNewStoreItems(IConfiguration configuration, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, SteamDbContext steamDb, SteamService steamService, ServiceBusClient serviceBus)
    {
        _configuration = configuration;
        _commandProcessor = commandProcessor;
        _queryProcessor = queryProcessor;
        _steamDb = steamDb;
        _steamService = steamService;
        _serviceBus = serviceBus;
    }

    [Function("Check-New-Store-Items")]
    public async Task Run([TimerTrigger("0 * * * * *")] /* every minute */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Check-New-Store-Items");

        var steamApps = await _steamDb.SteamApps
            .Where(x => x.IsActive)
            .ToListAsync();
        if (!steamApps.Any())
        {
            return;
        }

        var currencies = await _steamDb.SteamCurrencies.ToListAsync();
        if (currencies == null)
        {
            return;
        }

        var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_configuration.GetSteamConfiguration().ApplicationKey);
        var steamEconomy = steamWebInterfaceFactory.CreateSteamWebInterface<SteamEconomy>();
        foreach (var app in steamApps)
        {
            logger.LogTrace($"Checking for new store items (appId: {app.SteamId})");
            var response = await steamEconomy.GetAssetPricesAsync(
                uint.Parse(app.SteamId), string.Empty, Constants.SteamDefaultLanguage
            );
            if (response?.Data?.Success != true)
            {
                logger.LogError("Failed to get asset prices");
                continue;
            }

            if (app.Features.HasFlag(SteamAppFeatureTypes.StorePersistent) || app.Features.HasFlag(SteamAppFeatureTypes.StoreRotating))
            {
                // This app does item stores, check and add the items to the appropriate store instance
                await AddNewItemsToItemStore(logger, app, response, currencies);
            }
            else
            {
                // This app doesn't have item stores, but still check for and add any missing items
                await AddMissingStoreItems(logger, app, response, currencies);
            }
        }
    }

    private async Task AddNewItemsToItemStore(ILogger logger, SteamApp app, ISteamWebResponse<AssetPriceResultModel> assetPrices, IEnumerable<SteamCurrency> currencies)
    {
        // We want to compare the Steam item store with our most recent store
        var theirStoreItemIds = assetPrices.Data.Assets
            .Select(x => x.Name)
            .OrderBy(x => x)
            .Distinct()
            .ToList();
        var ourStoreItemIds = _steamDb.SteamItemStores
            .Where(x => x.AppId == app.Id)
            .Where(x => x.End == null)
            .OrderByDescending(x => x.Start)
            .SelectMany(x => x.Items.Where(i => i.Item.IsAvailable).Select(i => i.Item.SteamId))
            .Distinct()
            .ToList();

        // If both stores contain the same items, then there is no need to update anything
        var storesAreTheSame = ourStoreItemIds != null && theirStoreItemIds.All(x => ourStoreItemIds.Contains(x)) && ourStoreItemIds.All(x => theirStoreItemIds.Contains(x));
        if (storesAreTheSame)
        {
            return;
        }

        logger.LogInformation($"A store change was detected! (appId: {app.SteamId})");

        // If we got here, then then item store has changed (either added or removed items)
        // Load all of our active stores and their items
        var activeItemStores = _steamDb.SteamItemStores
            .Where(x => x.AppId == app.Id)
            .Where(x => x.End == null)
            .OrderByDescending(x => x.Start)
            .Include(x => x.Items).ThenInclude(x => x.Item)
            .Include(x => x.Items).ThenInclude(x => x.Item.Description)
            .ToList();
        var limitedItemsWereRemoved = false;
        foreach (var itemStore in activeItemStores.ToList())
        {
            var thisStoreItemIds = itemStore.Items.Select(x => x.Item.SteamId).ToList();
            var missingStoreItemIds = thisStoreItemIds.Where(x => !theirStoreItemIds.Contains(x));
            if (missingStoreItemIds.Any())
            {
                foreach (var missingStoreItemId in missingStoreItemIds)
                {
                    var missingStoreItem = itemStore.Items.FirstOrDefault(x => x.Item.SteamId == missingStoreItemId);
                    if (missingStoreItem != null)
                    {
                        missingStoreItem.Item.IsAvailable = false;
                        if (!missingStoreItem.Item.Description.IsPermanent)
                        {
                            limitedItemsWereRemoved = true;
                        }
                    }
                }
            }
            if (itemStore.Start != null && itemStore.Items.Any(x => !x.Item.IsAvailable) && limitedItemsWereRemoved)
            {
                itemStore.End = DateTimeOffset.UtcNow;
                activeItemStores.Remove(itemStore);
            }
        }

        // Ensure that an active "general" and "limited" item store exists
        var permanentItemStore = activeItemStores.FirstOrDefault(x => x.Start == null);
        if (permanentItemStore == null && app.Features.HasFlag(SteamAppFeatureTypes.StorePersistent))
        {
            permanentItemStore = new SteamItemStore()
            {
                App = app,
                AppId = app.Id,
                Name = "General"
            };
        }
        var limitedItemStore = activeItemStores.FirstOrDefault(x => x.Start != null);
        if ((limitedItemStore == null || limitedItemsWereRemoved) && app.Features.HasFlag(SteamAppFeatureTypes.StoreRotating))
        {
            limitedItemStore = new SteamItemStore()
            {
                App = app,
                AppId = app.Id,
                Start = DateTimeOffset.UtcNow
            };
        }

        // Check if there are any new items to be added to the stores
        var newPermanentStoreItems = new List<SteamStoreItemItemStore>();
        var newLimitedStoreItems = new List<SteamStoreItemItemStore>();
        var usdCurrency = currencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyUSD);
        foreach (var asset in assetPrices.Data.Assets)
        {
            // Ensure that the item is available in the database (create them if missing)
            var storeItem = await _steamService.AddOrUpdateStoreItemAndMarkAsAvailable(
                app, asset, usdCurrency, DateTimeOffset.Now
            );
            if (storeItem == null)
            {
                continue;
            }

            // Ensure that the item is linked to the store
            var itemStore = (storeItem.Description.IsPermanent || !app.Features.HasFlag(SteamAppFeatureTypes.StoreRotating)) ? permanentItemStore : limitedItemStore;
            if (!storeItem.Stores.Any(x => x.StoreId == itemStore.Id) && itemStore != null)
            {
                var prices = _steamService.ParseStoreItemPriceTable(asset.Prices);
                var storeItemLink = new SteamStoreItemItemStore()
                {
                    Store = itemStore,
                    Item = storeItem,
                    Currency = usdCurrency,
                    CurrencyId = usdCurrency.Id,
                    Price = prices.FirstOrDefault(x => x.Key == usdCurrency.Name).Value,
                    Prices = new PersistablePriceDictionary(prices),
                    IsPriceVerified = true
                };
                storeItem.Stores.Add(storeItemLink);
                itemStore.Items.Add(storeItemLink);
                if (itemStore == permanentItemStore)
                {
                    newPermanentStoreItems.Add(storeItemLink);
                }
                else if (itemStore == limitedItemStore)
                {
                    newLimitedStoreItems.Add(storeItemLink);
                }
            }

            // Update the store items "latest price"
            storeItem.UpdateLatestPrice();
        }

        // Regenerate store thumbnails (if items have changed)
        if (newPermanentStoreItems.Any() && permanentItemStore != null)
        {
            if (permanentItemStore.IsTransient)
            {
                _steamDb.SteamItemStores.Add(permanentItemStore);
            }
            if (permanentItemStore.Items.Any())
            {
                await RegenerateStoreItemsThumbnailImage(logger, app, permanentItemStore);
            }
        }
        if (newLimitedStoreItems.Any() && limitedItemStore != null)
        {
            if (limitedItemStore.IsTransient)
            {
                _steamDb.SteamItemStores.Add(limitedItemStore);
            }
            if (limitedItemStore.Items.Any())
            {
                await RegenerateStoreItemsThumbnailImage(logger, app, limitedItemStore);
            }
        }

        _steamDb.SaveChanges();

        // Send out a broadcast about any "new" items that weren't already in our store
        if (newPermanentStoreItems.Any())
        {
            logger.LogInformation($"{newPermanentStoreItems.Count} new permanent store items have been added!");
            await BroadcastStoreItemAddedMessages(logger, app, permanentItemStore, newPermanentStoreItems, currencies);
        }
        if (newLimitedStoreItems.Any())
        {
            logger.LogInformation($"{newLimitedStoreItems.Count} new limited store items have been added!");
            await BroadcastStoreItemAddedMessages(logger, app, limitedItemStore, newLimitedStoreItems, currencies);
        }
    }

    private async Task AddMissingStoreItems(ILogger logger, SteamApp app, ISteamWebResponse<AssetPriceResultModel> assetPrices, IEnumerable<SteamCurrency> currencies)
    {
        // We want to compare the Steam store items with our known store items
        var theirStoreItemIds = assetPrices.Data.Assets
            .Select(x => x.Name)
            .Distinct()
            .ToList();
        var ourStoreItemIds = _steamDb.SteamStoreItems
            .Where(x => x.AppId == app.Id)
            .Select(i => i.SteamId)
            .ToList();

        var missingAssets = assetPrices.Data.Assets
            .Where(x => !ourStoreItemIds.Contains(x.Name))
            .ToList();
        if (!missingAssets.Any())
        {
            return;
        }

        logger.LogInformation($"Missing store items were found! (appId: {app.SteamId})");

        // Add all missing store items
        var newStoreItems = new List<SteamStoreItem>();
        var usdCurrency = currencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyUSD);
        foreach (var asset in missingAssets)
        {
            // Ensure that the item is available in the database (create them if missing)
            var storeItem = await _steamService.AddOrUpdateStoreItemAndMarkAsAvailable(
                app, asset, usdCurrency, DateTimeOffset.Now
            );
            if (storeItem == null)
            {
                continue;
            }
            if (storeItem.IsTransient)
            {
                _steamDb.SteamStoreItems.Add(storeItem);
            }

            newStoreItems.Add(storeItem);
            _steamDb.SaveChanges();
        }

        _steamDb.SaveChanges();

        // Send out a broadcast about any "new" items
        if (newStoreItems.Any())
        {
            logger.LogInformation($"{newStoreItems.Count} new store items added!");
            //await BroadcastNewStoreItemsNotification(logger, app, null, newStoreItems, currencies);
        }
    }

    private async Task<string> RegenerateStoreItemsThumbnailImage(ILogger logger, SteamApp app, SteamItemStore store)
    {
        try
        {
            var itemImageSources = store.Items
                .Select(x => x.Item)
                .Where(x => x?.Description != null)
                .Select(x => new ImageSource()
                {
                    ImageUrl = x.Description.IconUrl,
                    ImageData = x.Description.Icon?.Data,
                })
                .ToList();

            var thumbnailImage = await _queryProcessor.ProcessAsync(new GetImageMosaicRequest()
            {
                ImageSources = itemImageSources,
                ImageSize = 128,
                ImageColumns = 3
            });

            if (thumbnailImage != null)
            {
                store.ItemsThumbnailUrl = (
                    await _commandProcessor.ProcessWithResultAsync(new UploadImageToBlobStorageRequest()
                    {
                        Name = $"{app.SteamId}-store-items-thumbnail-{Uri.EscapeDataString(store.Start?.Ticks.ToString() ?? store.Name?.ToLower())}",
                        MimeType = thumbnailImage.MimeType,
                        Data = thumbnailImage.Data,
                        ExpiresOn = null, // never
                        Overwrite = true
                    })
                )?.ImageUrl ?? store.ItemsThumbnailUrl;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate store item thumbnail image");
        }

        return store.ItemsThumbnailUrl;
    }

    private async Task BroadcastStoreItemAddedMessages(ILogger logger, SteamApp app, SteamItemStore store, IEnumerable<SteamStoreItemItemStore> newStoreItems, IEnumerable<SteamCurrency> currencies)
    {
        newStoreItems = newStoreItems?.OrderBy(x => x.Item?.Description?.Name);
        if (newStoreItems?.Any() != true)
        {
            return;
        }

        var broadcastTasks = new List<Task>();
        foreach (var storeItem in newStoreItems)
        {
            broadcastTasks.Add(
                _serviceBus.SendMessageAsync(new StoreItemAddedMessage()
                {
                    AppId = UInt64.Parse(app.SteamId),
                    AppName = app.Name,
                    AppIconUrl = app.IconUrl,
                    AppColour = app.PrimaryColor,
                    StoreId = store.StoreId(),
                    StoreName = store.StoreName(),
                    CreatorId = storeItem.Item.Description?.CreatorId,
                    CreatorName = storeItem.Item.Description?.CreatorProfile?.Name,
                    CreatorAvatarUrl = storeItem.Item.Description?.CreatorProfile?.AvatarUrl,
                    ItemId = UInt64.Parse(storeItem.Item.SteamId),
                    ItemType = storeItem.Item.Description?.ItemType,
                    ItemShortName = storeItem.Item.Description?.ItemShortName,
                    ItemName = storeItem.Item.Description?.Name,
                    ItemDescription = storeItem.Item.Description?.Description,
                    ItemCollection = storeItem.Item.Description?.ItemCollection,
                    ItemImageUrl = storeItem.Item.Description?.IconLargeUrl ?? storeItem.Item.Description?.IconUrl,
                    ItemPrices = storeItem.Prices.Select(x => new StoreItemAddedMessage.Price()
                    { 
                        Currency = x.Key,
                        Value = x.Value,
                        Description = currencies.FirstOrDefault(c => c.Name == x.Key)?.ToPriceString(x.Value, dense: true)
                    }).ToArray()
                })
            );
        }

        await Task.WhenAll(broadcastTasks);
    }
}
