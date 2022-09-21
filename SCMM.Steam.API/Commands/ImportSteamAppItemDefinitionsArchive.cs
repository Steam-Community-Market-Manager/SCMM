using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Azure.ServiceBus;
using SCMM.Shared.API.Events;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.API.Extensions;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.WebApi.Models;
using SCMM.Steam.Data.Models.WebApi.Requests.IGameInventory;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;
using Steam.Models.SteamEconomy;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace SCMM.Steam.API.Commands
{
    public class ImportSteamAppItemDefinitionsArchiveRequest : ICommand
    {
        public string AppId { get; set; }

        public string ItemDefinitionsDigest { get; set; }

        /// <summary>
        /// If true, item definition will be parsed (for asset descriptions, store items, and market items)
        /// </summary>
        public bool ParseChanges { get; set; } = false;
    }

    public class ImportSteamAppItemDefinitionsArchive : ICommandHandler<ImportSteamAppItemDefinitionsArchiveRequest>
    {
        private readonly ILogger<ImportSteamAppItemDefinitionsArchive> _logger;
        private readonly SteamConfiguration _steamConfiguration;
        private readonly SteamDbContext _steamDb;
        private readonly SteamWebApiClient _steamApiClient;
        private readonly SteamCommunityWebClient _steamCommunityClient;
        private readonly ServiceBusClient _serviceBus;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public ImportSteamAppItemDefinitionsArchive(ILogger<ImportSteamAppItemDefinitionsArchive> logger, SteamConfiguration steamConfiguration, SteamDbContext steamDb, SteamWebApiClient steamApiClient, SteamCommunityWebClient steamCommunityClient, ServiceBusClient serviceBus, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _logger = logger;
            _steamConfiguration = steamConfiguration;
            _steamDb = steamDb;
            _steamApiClient = steamApiClient;
            _steamCommunityClient = steamCommunityClient;
            _serviceBus = serviceBus;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
        }

        public async Task HandleAsync(ImportSteamAppItemDefinitionsArchiveRequest request)
        {
            // Get the item definition archive
            var itemDefinitionsArchive = await _steamApiClient.GameInventoryGetItemDefArchive(new GetItemDefArchiveJsonRequest()
            {
                AppId = UInt64.Parse(request.AppId),
                Digest = request.ItemDefinitionsDigest,
            });
            if (itemDefinitionsArchive == null || !itemDefinitionsArchive.Any())
            {
                return;
            }

            // Get the app
            var app = await _steamDb.SteamApps.FirstOrDefaultAsync(x => x.SteamId == request.AppId);
            if (app == null)
            {
                return;
            }

            // Add the item definitions archive to the app (if missing)
            var itemDefinitionsArchiveAlreadyExists = await _steamDb.SteamItemDefinitionsArchive.AnyAsync(x => x.AppId == app.Id && x.Digest == request.ItemDefinitionsDigest);
            if (!itemDefinitionsArchiveAlreadyExists)
            {
                _steamDb.SteamItemDefinitionsArchive.Add(new SteamItemDefinitionsArchive()
                {
                    App = app,
                    Digest = request.ItemDefinitionsDigest,
                    ItemDefinitions = JsonSerializer.Serialize(itemDefinitionsArchive.ToArray()),
                    TimePublished = DateTimeOffset.Now
                });

                await _steamDb.SaveChangesAsync();
            }

            if (request.ParseChanges)
            {
                // TODO: Filter this properly
                var itemDefinitions = itemDefinitionsArchive
                    .Where(x => x.Name != "DELETED" && x.Type != "generator")
                    .ToArray();

                var currencies = await _steamDb.SteamCurrencies.ToArrayAsync();
                var assetDescriptions = await _steamDb.SteamAssetDescriptions
                    .Include(x => x.App)
                    .Include(x => x.CreatorProfile)
                    .Include(x => x.StoreItem)
                    .Include(x => x.MarketItem)
                    .Where(x => x.AppId == app.Id)
                    .ToListAsync();

                // Parse all item definition changes in the archive
                _logger.LogInformation($"Parsing item definitions for missing ids (appId: {app.SteamId}, digest: '{request.ItemDefinitionsDigest}')");
                await LinkAssetDescriptionsToItemDefinitionsFromArchive(app, itemDefinitions, assetDescriptions);

                _logger.LogInformation($"Parsing item definitions for new asset descriptions (appId: {app.SteamId}, digest: '{request.ItemDefinitionsDigest}')");
                await AddNewAssetDescriptionsFromArchive(app, itemDefinitions, assetDescriptions);
                await _steamDb.SaveChangesAsync();

                _logger.LogInformation($"Parsing item definitions for store item changes (appId: {app.SteamId}, digest: '{request.ItemDefinitionsDigest}')");
                await AddOrUpdateStoreItemsFromArchive(app, itemDefinitions, assetDescriptions, currencies);
                await _steamDb.SaveChangesAsync();

                _logger.LogInformation($"Parsing item definitions for market item changes (appId: {app.SteamId}, digest: '{request.ItemDefinitionsDigest}')");
                await AddOrUpdateMarketItemsFromArchive(app, itemDefinitions, assetDescriptions, currencies);
                await _steamDb.SaveChangesAsync();

                _logger.LogInformation($"Parsing item definitions for updated asset descriptions (appId: {app.SteamId}, digest: '{request.ItemDefinitionsDigest}')");
                await UpdateExistingAssetDescriptionsFromArchive(app, itemDefinitions, assetDescriptions);
                await _steamDb.SaveChangesAsync();
            }
        }

        private async Task LinkAssetDescriptionsToItemDefinitionsFromArchive(SteamApp app, IEnumerable<ItemDefinition> itemDefinitions, ICollection<SteamAssetDescription> assetDescriptions)
        {
            var assetDescriptionsWithMissingItemDefinitionId = assetDescriptions
                .Where(x => x.ItemDefinitionId == null)
                .ToArray();

            foreach (var assetDescription in assetDescriptionsWithMissingItemDefinitionId)
            {
                // It is possible that asset descriptions are added without an item id, so link them to the item definitions when possible
                var itemDefinition = itemDefinitions.FirstOrDefault(x =>
                    (assetDescription.ItemDefinitionId > 0 && x.ItemDefId > 0 && assetDescription.ItemDefinitionId == x.ItemDefId) ||
                    (assetDescription.WorkshopFileId > 0 && x.WorkshopId > 0 && assetDescription.WorkshopFileId == x.WorkshopId) ||
                    (!String.IsNullOrEmpty(assetDescription.NameHash) && !String.IsNullOrEmpty(x.MarketHashName) && assetDescription.NameHash == x.MarketHashName) ||
                    (!String.IsNullOrEmpty(assetDescription.Name) && !String.IsNullOrEmpty(x.MarketName) && assetDescription.Name == x.MarketName) ||
                    (!String.IsNullOrEmpty(assetDescription.Name) && !String.IsNullOrEmpty(x.Name) && assetDescription.Name == x.Name)
                );
                if (itemDefinition != null)
                {
                    assetDescription.ItemDefinitionId = itemDefinition.ItemDefId;
                }
            }
        }

        private async Task AddNewAssetDescriptionsFromArchive(SteamApp app, IEnumerable<ItemDefinition> itemDefinitions, ICollection<SteamAssetDescription> assetDescriptions)
        {
            var newItemDefinitions = itemDefinitions
                .Where(x => !assetDescriptions.Any(y => y.ItemDefinitionId == x.ItemDefId))
                .OrderBy(x => x.Name)
                .ToArray();

            foreach (var itemDefinition in newItemDefinitions)
            {
                // Add the new asset description
                _logger.LogInformation($"A new item definition was found! (appId: {app.SteamId}, itemId: {itemDefinition.ItemDefId}, name: '{itemDefinition.Name}')");
                var importedAssetDescription = await _commandProcessor.ProcessWithResultAsync(new ImportSteamItemDefinitionRequest()
                {
                    AppId = UInt64.Parse(app.SteamId),
                    ItemDefinitionId = itemDefinition.ItemDefId,
                    ItemDefinitionName = itemDefinition.Name,
                    ItemDefinition = itemDefinition
                });
                var newAssetDescription = importedAssetDescription?.AssetDescription;
                if (newAssetDescription != null)
                {
                    assetDescriptions.Add(newAssetDescription);
                    await _serviceBus.SendMessageAsync(new ItemDefinitionAddedMessage()
                    {
                        AppId = UInt64.Parse(app.SteamId),
                        AppName = app.Name,
                        AppIconUrl = app.IconUrl,
                        AppColour = app.PrimaryColor,
                        CreatorId = newAssetDescription.CreatorId,
                        CreatorName = newAssetDescription.CreatorProfile?.Name,
                        CreatorAvatarUrl = newAssetDescription.CreatorProfile?.AvatarUrl,
                        ItemId = newAssetDescription.ItemDefinitionId ?? 0,
                        ItemType = newAssetDescription.ItemType,
                        ItemShortName = newAssetDescription.ItemShortName,
                        ItemName = newAssetDescription.Name,
                        ItemDescription = newAssetDescription.Description,
                        ItemCollection = newAssetDescription.ItemCollection,
                        ItemIconUrl = newAssetDescription.IconUrl ?? newAssetDescription.IconLargeUrl,
                        ItemImageUrl = newAssetDescription.PreviewUrl ?? newAssetDescription.IconLargeUrl ?? newAssetDescription.IconUrl,
                    });
                }
            }
        }

        private async Task UpdateExistingAssetDescriptionsFromArchive(SteamApp app, IEnumerable<ItemDefinition> itemDefinitions, ICollection<SteamAssetDescription> assetDescriptions)
        {
            var items = itemDefinitions
                .Join(assetDescriptions, x => x.ItemDefId, y => y.ItemDefinitionId, (x, y) => new
                {
                    ItemDefinition = x,
                    AssetDescription = y
                })
                .OrderBy(x => x.ItemDefinition.Name)
                .ToArray();

            foreach (var item in items.Where(x => x.AssetDescription != null && x.ItemDefinition != null))
            {
                await _commandProcessor.ProcessWithResultAsync(new UpdateSteamAssetDescriptionRequest()
                {
                    AssetDescription = item.AssetDescription,
                    AssetItemDefinition = item.ItemDefinition,
                    SkipItemCollectionCheck = true
                });
            }
        }

        private async Task AddOrUpdateStoreItemsFromArchive(SteamApp app, IEnumerable<ItemDefinition> itemDefinitions, ICollection<SteamAssetDescription> assetDescriptions, IEnumerable<SteamCurrency> currencies)
        {
            var storeItems = itemDefinitions
                .Join(assetDescriptions, x => x.ItemDefId, y => y.ItemDefinitionId, (x, y) => new
                {
                    ItemDefinition = x,
                    AssetDescription = y,
                    HasBeenRemovedFromStore = (String.IsNullOrEmpty(x.PriceCategory) && (y.StoreItem?.IsAvailable == true)),
                    HasBeenAddedToStore = (!String.IsNullOrEmpty(x.PriceCategory) && (y.StoreItem?.IsAvailable != true))
                })
                .OrderBy(x => x.ItemDefinition.Name)
                .ToArray();

            // Update all store items that are no longer available
            foreach (var removedStoreItem in storeItems.Where(x => x.HasBeenRemovedFromStore && x.AssetDescription?.StoreItem != null))
            {
                _logger.LogInformation($"An item has been removed from the store! (appId: {app.SteamId}, itemId: {removedStoreItem.ItemDefinition.ItemDefId}, name: '{removedStoreItem.ItemDefinition.Name}')");
                removedStoreItem.AssetDescription.StoreItem.IsAvailable = false;
            }

            // Add all newly added store items
            var addedStoreItems = storeItems.Where(x => x.HasBeenAddedToStore);
            if (addedStoreItems.Any())
            {
                // Get the latest asset description prices
                var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_steamConfiguration.ApplicationKey);
                var steamEconomy = steamWebInterfaceFactory.CreateSteamWebInterface<SteamEconomy>();
                var defaultCurrency = currencies.FirstOrDefault(x => x.Name == Constants.SteamDefaultCurrency);
                var assetPricesResponse = await steamEconomy.GetAssetPricesAsync(uint.Parse(app.SteamId));
                if (assetPricesResponse?.Data?.Success != true)
                {
                    _logger.LogError($"Failed to get store asset prices (appId: {app.SteamId})");
                }

                // TODO: Validate that new store items are present in asset price list, else loop and retry

                var permanentItemStore = (SteamItemStore) null;
                var limitedItemStore = (SteamItemStore)null;

                // If the app uses a permanent or limited item store, check that they are still available (or create new ones)
                if (app.Features.HasFlag(SteamAppFeatureTypes.StorePersistent) || app.Features.HasFlag(SteamAppFeatureTypes.StoreRotating))
                {
                    // Load all of our [active] stores
                    var activeItemStores = await _steamDb.SteamItemStores
                        .Where(x => x.AppId == app.Id)
                        .Where(x => x.End == null)
                        .OrderByDescending(x => x.Start)
                        .Include(x => x.Items).ThenInclude(x => x.Item)
                        .Include(x => x.Items).ThenInclude(x => x.Item.Description)
                        .ToListAsync();

                    // If all items in a store are no longer available, the store is no longer active
                    foreach (var activeItemStore in activeItemStores.ToArray())
                    {
                        if (activeItemStore.Items.All(x => !x.Item.IsAvailable))
                        {
                            _logger.LogInformation($"An item store is now unavailable, no [available] items remaining (appId: {app.SteamId}, storeId: {activeItemStore.StoreId()})");
                            activeItemStore.End = DateTimeOffset.UtcNow;
                            activeItemStores.Remove(activeItemStore);
                        }
                    }

                    // Ensure that an active "permanent" item store exists
                    if (app.Features.HasFlag(SteamAppFeatureTypes.StorePersistent))
                    {
                        permanentItemStore = activeItemStores.FirstOrDefault(x => x.Start == null);
                        if (permanentItemStore == null)
                        {
                            _steamDb.SteamItemStores.Add(permanentItemStore = new SteamItemStore()
                            {
                                App = app,
                                AppId = app.Id,
                                Name = "General"
                            });
                            _logger.LogInformation($"A new permanent item store was added (appId: {app.SteamId}, storeId: {permanentItemStore.StoreId()})");
                        }
                    }

                    // Ensure that an active "limited" item store exists
                    if (app.Features.HasFlag(SteamAppFeatureTypes.StoreRotating))
                    {
                        // If any items in the limited store are no longer available, rotate it (i.e. create a new limited store)
                        limitedItemStore = activeItemStores.FirstOrDefault(x => x.Start != null);
                        if (limitedItemStore == null || limitedItemStore.Items.Any(x => !x.Item.IsAvailable))
                        {
                            _steamDb.SteamItemStores.Add(limitedItemStore = new SteamItemStore()
                            {
                                App = app,
                                AppId = app.Id,
                                Start = DateTimeOffset.UtcNow
                            });
                            _logger.LogInformation($"A new limited item store was added (appId: {app.SteamId}, storeId: {limitedItemStore.StoreId()})");
                        }
                    }
                }

                var newStoreItems = new List<SteamStoreItem>();
                foreach (var addedStoreItem in addedStoreItems)
                {
                    var itemDefinition = addedStoreItem.ItemDefinition;
                    var assetDescription = addedStoreItem.AssetDescription;
                    var storeType = addedStoreItem.AssetDescription.IsPermanent ? "permanent" : "limited";
                    var store = addedStoreItem.AssetDescription.IsPermanent ? permanentItemStore : limitedItemStore;
                    
                    _logger.LogInformation($"A new item has been added to the {storeType} item store! (appId: {app.SteamId}, itemId: {addedStoreItem.ItemDefinition.ItemDefId}, name: '{addedStoreItem.ItemDefinition.Name}')");
                    
                    var storeItemAsset = assetPricesResponse?.Data?.Assets?.FirstOrDefault(x => x.ClassId == assetDescription.ClassId || x.Class?.Any(y => y.Value == itemDefinition.ItemDefId.ToString()) == true);
                    var storeItemPrices = storeItemAsset?.Prices?.ToDictionary();
                    var storeItem = await CreateOrUpdateStoreItemAndMarkAsAvailable(app, assetDescription, storeItemAsset, defaultCurrency, DateTimeOffset.Now);
                    if (storeItem == null)
                    {
                        continue;
                    }

                    // Ensure that the item is linked to the store (if any)
                    if (store != null && !storeItem.Stores.Any(x => x.StoreId == store.Id))
                    {
                        var storeItemLink = new SteamStoreItemItemStore()
                        {
                            Store = store,
                            Item = storeItem,
                            Currency = defaultCurrency,
                            CurrencyId = defaultCurrency.Id,
                            Price = storeItemPrices?.FirstOrDefault(x => x.Key == defaultCurrency.Name).Value,
                            Prices = (storeItemPrices != null ? new PersistablePriceDictionary(storeItemPrices) : new PersistablePriceDictionary()),
                            IsPriceVerified = true
                        };
                        storeItem.Stores.Add(storeItemLink);
                        store.Items.Add(storeItemLink);
                    }

                    // Update the store items "latest price"
                    storeItem.UpdateLatestPrice();

                    // Reimport the asset description to get the latest item info
                    if (assetDescription.ClassId > 0)
                    {
                        await _steamDb.SaveChangesAsync();
                        await _commandProcessor.ProcessWithResultAsync(new ImportSteamAssetDescriptionRequest()
                        {
                            AppId = UInt64.Parse(app.SteamId),
                            AssetClassId = assetDescription.ClassId.Value
                        });
                    }

                    await _serviceBus.SendMessageAsync(new StoreItemAddedMessage()
                    {
                        AppId = UInt64.Parse(app.SteamId),
                        AppName = app.Name,
                        AppIconUrl = app.IconUrl,
                        AppColour = app.PrimaryColor,
                        StoreId = store?.StoreId(),
                        StoreName = store?.StoreName(),
                        CreatorId = storeItem.Description?.CreatorId,
                        CreatorName = storeItem.Description?.CreatorProfile?.Name,
                        CreatorAvatarUrl = storeItem.Description?.CreatorProfile?.AvatarUrl,
                        ItemId = UInt64.Parse(storeItem.SteamId),
                        ItemType = storeItem.Description?.ItemType,
                        ItemShortName = storeItem.Description?.ItemShortName,
                        ItemName = storeItem.Description?.Name,
                        ItemDescription = storeItem.Description?.Description,
                        ItemCollection = storeItem.Description?.ItemCollection,
                        ItemIconUrl = storeItem.Description?.IconUrl ?? storeItem.Description?.IconLargeUrl,
                        ItemImageUrl = storeItem.Description?.PreviewUrl ?? storeItem.Description?.IconLargeUrl ?? storeItem.Description?.IconUrl,
                        ItemPrices = storeItem.Prices.Select(x => new StoreItemAddedMessage.Price()
                        {
                            Currency = x.Key,
                            Value = x.Value,
                            Description = currencies.FirstOrDefault(c => c.Name == x.Key)?.ToPriceString(x.Value, dense: true)
                        }).ToArray()
                    });

                    newStoreItems.Add(storeItem);
                }

                var newPermanentStoreItems = newStoreItems.Where(x => x.Description.IsPermanent).ToArray();
                if (permanentItemStore != null && newPermanentStoreItems.Any())
                {
                    await RegenerateStoreItemsThumbnailAndSendStoreAddedMessage(app, permanentItemStore, newPermanentStoreItems, defaultCurrency);
                }
                var newLimitedStoreItems = newStoreItems.Where(x => !x.Description.IsPermanent).ToArray();
                if (limitedItemStore != null && newLimitedStoreItems.Any())
                {
                    await RegenerateStoreItemsThumbnailAndSendStoreAddedMessage(app, limitedItemStore, newLimitedStoreItems, defaultCurrency);
                }
            }
        }

        private async Task<SteamStoreItem> CreateOrUpdateStoreItemAndMarkAsAvailable(SteamApp app, SteamAssetDescription assetDescription, AssetModel asset, SteamCurrency currency, DateTimeOffset? timeChecked)
        {
            if (assetDescription.ClassId == null && asset.ClassId > 0)
            {
                assetDescription.ClassId = asset.ClassId;
            }

            // Reload store item from database (if it exists) to ensure we load all store instances this item appears in
            var storeItem = await _steamDb.SteamStoreItems
                .Include(x => x.Stores).ThenInclude(x => x.Store)
                .Include(x => x.Description)
                .Include(x => x.Description.App)
                .Include(x => x.Description.CreatorProfile)
                .FirstOrDefaultAsync(x => x.AppId == app.Id && x.Description.ClassId == asset.ClassId);

            // If the store item doesn't exist yet, create it now
            if (storeItem == null)
            {
                app.StoreItems.Add(storeItem = assetDescription.StoreItem = new SteamStoreItem()
                {
                    App = app,
                    AppId = app.Id,
                    Description = assetDescription,
                    DescriptionId = assetDescription.Id
                });

                var prices = asset.Prices.ToDictionary();
                storeItem.UpdatePrice(
                    currency,
                    prices.FirstOrDefault(x => x.Key == currency?.Name).Value,
                    new PersistablePriceDictionary(prices)
                );
            }

            // If the asset item is not yet accepted, accept it now
            assetDescription.IsAccepted = true;
            if (assetDescription.TimeAccepted == null)
            {
                if (!String.IsNullOrEmpty(asset.Date))
                {
                    DateTimeOffset storeDate;
                    if (DateTimeOffset.TryParseExact(asset.Date, "yyyy-M-d", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out storeDate) ||
                        DateTimeOffset.TryParseExact(asset.Date, "yyyy/M/d", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out storeDate))
                    {
                        assetDescription.TimeAccepted = storeDate;
                    }
                }
                else
                {
                    assetDescription.TimeAccepted = timeChecked;
                }
            }

            // Mark the store item as available
            storeItem.SteamId = asset.Name;
            storeItem.IsAvailable = true;
            return storeItem;
        }

        private async Task AddOrUpdateMarketItemsFromArchive(SteamApp app, IEnumerable<ItemDefinition> itemDefinitions, ICollection<SteamAssetDescription> assetDescriptions, IEnumerable<SteamCurrency> currencies)
        {
            var marketItems = itemDefinitions
                .Join(assetDescriptions, x => x.ItemDefId, y => y.ItemDefinitionId, (x, y) => new
                {
                    ItemDefinition = x,
                    AssetDescription = y,
                    HasBecomeUnmarketable = (!x.Marketable && y.IsMarketable),
                    HasBecomeMarketable = (x.Marketable && !y.IsMarketable)
                })
                .OrderBy(x => x.ItemDefinition.Name)
                .ToArray();

            // Update all market items that are no longer available
            foreach (var newUnmarketableItem in marketItems.Where(x => x.HasBecomeUnmarketable && x.AssetDescription != null))
            {
                // TODO: Handle this?
            }

            // Add all newly added market items
            foreach (var newMarketableItem in marketItems.Where(x => x.HasBecomeMarketable && x.AssetDescription != null))
            {
                try
                {
                    var itemDefinition = newMarketableItem.ItemDefinition;
                    var assetDescription = newMarketableItem.AssetDescription;
                    
                    _logger.LogInformation($"An new item has been become marketable! (appId: {app.SteamId}, itemId: {itemDefinition.ItemDefId}, name: '{itemDefinition.Name}')");

                    // Double check that this asset description can currently be listed on the Steam community market
                    var marketPriceOverviewResponse = await _steamCommunityClient.GetMarketPriceOverview(new SteamMarketPriceOverviewJsonRequest()
                    {
                        AppId = app.SteamId,
                        MarketHashName = assetDescription.NameHash,
                        NoRender = true
                    });

                    if (marketPriceOverviewResponse?.Success != true)
                    {
                        continue;
                    }

                    // Add a new market item for this asset description
                    var defaultCurrency = currencies.FirstOrDefault(x => x.Name == Constants.SteamDefaultCurrency);
                    var marketItem = assetDescription.MarketItem;
                    if (marketItem == null)
                    {
                        app.MarketItems.Add(marketItem = assetDescription.MarketItem = new SteamMarketItem()
                        {
                            SteamId = assetDescription.NameId?.ToString(),
                            AppId = app.Id,
                            App = app,
                            Description = assetDescription,
                            Currency = defaultCurrency,
                        });
                    }

                    // Reimport the asset description to get the latest item info-- specifically, the Steam community market "name id"
                    if (assetDescription.ClassId > 0)
                    {
                        await _steamDb.SaveChangesAsync();
                        var importedAssetDescription = await _commandProcessor.ProcessWithResultAsync(new ImportSteamAssetDescriptionRequest()
                        {
                            AppId = UInt64.Parse(app.SteamId),
                            AssetClassId = assetDescription.ClassId.Value
                        });
                    }

                    await _serviceBus.SendMessageAsync(new MarketItemAddedMessage()
                    {
                        AppId = UInt64.Parse(app.SteamId),
                        AppName = app.Name,
                        AppIconUrl = app.IconUrl,
                        AppColour = app.PrimaryColor,
                        CreatorId = marketItem.Description?.CreatorId,
                        CreatorName = marketItem.Description?.CreatorProfile?.Name,
                        CreatorAvatarUrl = marketItem.Description?.CreatorProfile?.AvatarUrl,
                        ItemId = UInt64.Parse(marketItem.SteamId),
                        ItemType = marketItem.Description?.ItemType,
                        ItemShortName = marketItem.Description?.ItemShortName,
                        ItemName = marketItem.Description?.Name,
                        ItemDescription = marketItem.Description?.Description,
                        ItemCollection = marketItem.Description?.ItemCollection,
                        ItemIconUrl = marketItem.Description?.IconUrl ?? marketItem.Description?.IconLargeUrl,
                        ItemImageUrl = marketItem.Description?.PreviewUrl ?? marketItem.Description?.IconLargeUrl ?? marketItem.Description?.IconUrl,
                    });
                }
                catch (SteamRequestException ex)
                {
                    if (ex.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                    {
                        // This means the item cannot be listed on the Steam community market yet, ignore for now...
                        _logger.LogWarning($"Item definition claims item is now marketable, but SCM is not allowing market listings yet (appId: {app.SteamId}, itemId: {newMarketableItem.ItemDefinition.ItemDefId}, name: '{newMarketableItem.ItemDefinition.Name}')");
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private async Task RegenerateStoreItemsThumbnailAndSendStoreAddedMessage(SteamApp app, SteamItemStore store, IEnumerable<SteamStoreItem> newItems, SteamCurrency currency)
        {
            try
            {
                // Regenerate store items thumbnail image
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
                _logger.LogError(ex, $"Failed to generate store item thumbnail image (appId: {app.SteamId}, storeId: {store.StoreId()})");
            }

            if (newItems.Any())
            {
                await _serviceBus.SendMessageAsync(new StoreAddedMessage()
                {
                    AppId = UInt64.Parse(app.SteamId),
                    AppName = app.Name,
                    AppIconUrl = app.IconUrl,
                    AppColour = app.PrimaryColor,
                    StoreId = store.StoreId(),
                    StoreName = store.StoreName(),
                    Items = newItems.Select(x => new StoreAddedMessage.Item()
                    {
                        Name = x.Description?.Name,
                        Currency = currency.Name,
                        Price = x.Price,
                        PriceDescription = x.Price != null ? currency?.ToPriceString(x.Price.Value) : null
                    }).ToArray(),
                    ItemsImageUrl = store.ItemsThumbnailUrl
                });
            }
        }
    }
}
