using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Azure.ServiceBus;
using SCMM.Shared.API.Events;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
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
            var app = _steamDb.SteamApps.FirstOrDefault(x => x.SteamId == request.AppId);
            if (app == null)
            {
                return;
            }

            // Add the item definitions archive to the app (if missing)
            app.ItemDefinitionArchives.Add(new SteamItemDefinitionsArchive()
            {
                App = app,
                Digest = request.ItemDefinitionsDigest,
                ItemDefinitions = JsonSerializer.Serialize(itemDefinitionsArchive.ToArray()),
                TimePublished = DateTimeOffset.Now
            });

            await _steamDb.SaveChangesAsync();

            var currencies = await _steamDb.SteamCurrencies.ToArrayAsync();
            var assetDescriptions = await _steamDb.SteamAssetDescriptions
                .Include(x => x.App)
                .Include(x => x.CreatorProfile)
                .Include(x => x.StoreItem)
                .Include(x => x.MarketItem)
                .Where(x => x.AppId == app.Id)
                .ToListAsync();

            // Parse all item definition changes in the archive
            await AddOrUpdateAssetDescriptionsFromArchive(app, itemDefinitionsArchive, assetDescriptions);
            await _steamDb.SaveChangesAsync();
            await AddNewStoreItemsFromArchive(app, itemDefinitionsArchive, assetDescriptions, currencies);
            await _steamDb.SaveChangesAsync();
            await AddNewMarketItemsFromArchive(app, itemDefinitionsArchive, assetDescriptions, currencies);
            await _steamDb.SaveChangesAsync();
        }

        private async Task AddOrUpdateAssetDescriptionsFromArchive(SteamApp app, IEnumerable<ItemDefinition> itemDefinitions, ICollection<SteamAssetDescription> assetDescriptions)
        {
            // TODO: Filter this properly
            var fileredItemDefinitions = itemDefinitions
                .Where(x => x.Name != "DELETED" && x.Type != "generator");

            foreach (var itemDefinition in fileredItemDefinitions)
            {
                var assetDescription = assetDescriptions.FirstOrDefault(x =>
                    (x.ItemDefinitionId > 0 && itemDefinition.ItemDefId > 0 && x.ItemDefinitionId == itemDefinition.ItemDefId) ||
                    (x.WorkshopFileId > 0 && itemDefinition.WorkshopId > 0 && x.WorkshopFileId == itemDefinition.WorkshopId) ||
                    (!String.IsNullOrEmpty(x.NameHash) && !String.IsNullOrEmpty(itemDefinition.MarketHashName) && x.NameHash == itemDefinition.MarketHashName) ||
                    (!String.IsNullOrEmpty(x.Name) && !String.IsNullOrEmpty(itemDefinition.MarketName) && x.Name == itemDefinition.MarketName) ||
                    (!String.IsNullOrEmpty(x.Name) && !String.IsNullOrEmpty(itemDefinition.Name) && x.Name == itemDefinition.Name)
                );
                if (assetDescription == null)
                {
                    // Add the new asset description
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
                            ItemImageUrl = newAssetDescription.PreviewUrl ?? newAssetDescription.IconLargeUrl ?? newAssetDescription.IconUrl,
                        });
                    }
                }
                else
                {
                    // Update the existing asset description
                    var updatedAssetDescription = await _commandProcessor.ProcessWithResultAsync(new UpdateSteamAssetDescriptionRequest()
                    {
                        AssetDescription = assetDescription,
                        AssetItemDefinition = itemDefinition
                    });
                }
            }
        }

        private async Task AddNewStoreItemsFromArchive(SteamApp app, IEnumerable<ItemDefinition> itemDefinitions, ICollection<SteamAssetDescription> assetDescriptions, IEnumerable<SteamCurrency> currencies)
        {
            var storeItems = itemDefinitions
                .Join(assetDescriptions, x => x.ItemDefId, y => y.ItemDefinitionId, (x, y) => new
                {
                    ItemDefinition = x,
                    AssetDescription = y,
                    HasBeenRemovedFromStore = (String.IsNullOrEmpty(x.PriceCategory) && (y.StoreItem?.IsAvailable == true)),
                    HasBeenAddedToStore = (!String.IsNullOrEmpty(x.PriceCategory) && (y.StoreItem?.IsAvailable != true))
                })
                .ToArray();

            // Get the latest asset description prices
            var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_steamConfiguration.ApplicationKey);
            var steamEconomy = steamWebInterfaceFactory.CreateSteamWebInterface<SteamEconomy>();
            var assetPricesResponse = await steamEconomy.GetAssetPricesAsync(uint.Parse(app.SteamId));
            if (assetPricesResponse?.Data?.Success != true)
            {
                _logger.LogError($"Failed to get asset prices (appId: {app.SteamId})");
            }

            // Does this app have item stores? check and add the items to the appropriate store instance
            if (app.Features.HasFlag(SteamAppFeatureTypes.StorePersistent) || app.Features.HasFlag(SteamAppFeatureTypes.StoreRotating))
            {
                await AddNewItemsToItemStore(app, assetPricesResponse, currencies);
            }

            // Else, this app doesn't have item stores, but still check for and add any missing items
            else
            {
                foreach (var removedStoreItem in storeItems.Where(x => x.HasBeenRemovedFromStore))
                {
                    // TODO: Handle this...
                }

                foreach (var addedStoreItem in storeItems.Where(x => x.HasBeenAddedToStore))
                {
                    // TODO: Handle this...
                }
            }
        }

        private async Task AddNewItemsToItemStore(SteamApp app, ISteamWebResponse<AssetPriceResultModel> assetPrices, IEnumerable<SteamCurrency> currencies)
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

            _logger.LogInformation($"A store change was detected! (appId: {app.SteamId})");

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
                var storeItem = await AddOrUpdateStoreItemAndMarkAsAvailable(
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
                    var prices = ParseStoreItemPriceTable(asset.Prices);
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
                    await RegenerateStoreItemsThumbnailImage(app, permanentItemStore);
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
                    await RegenerateStoreItemsThumbnailImage(app, limitedItemStore);
                }
            }

            await _steamDb.SaveChangesAsync();

            // Send out a broadcast about any "new" items that weren't already in our store
            if (newPermanentStoreItems.Any())
            {
                _logger.LogInformation($"{newPermanentStoreItems.Count} new permanent store items have been added!");
                await BroadcastStoreItemAddedMessages(app, permanentItemStore, newPermanentStoreItems, currencies);
            }
            if (newLimitedStoreItems.Any())
            {
                _logger.LogInformation($"{newLimitedStoreItems.Count} new limited store items have been added!");
                await BroadcastStoreItemAddedMessages(app, limitedItemStore, newLimitedStoreItems, currencies);
            }
        }

        private async Task<SteamStoreItem> AddOrUpdateStoreItemAndMarkAsAvailable(SteamApp app, AssetModel asset, SteamCurrency currency, DateTimeOffset? timeChecked)
        {
            // Find the item by it's store id or asset class id (which ever exists first)
            var dbItem = (
                await _steamDb.SteamStoreItems
                    .Include(x => x.Stores).ThenInclude(x => x.Store)
                    .Include(x => x.Description)
                    .Include(x => x.Description.App)
                    .Include(x => x.Description.CreatorProfile)
                    .Where(x => x.AppId == app.Id)
                    .FirstOrDefaultAsync(x => x.SteamId == asset.Name) ??
                await _steamDb.SteamStoreItems
                    .Include(x => x.Stores).ThenInclude(x => x.Store)
                    .Include(x => x.Description)
                    .Include(x => x.Description.App)
                    .Include(x => x.Description.CreatorProfile)
                    .Where(x => x.AppId == app.Id)
                    .FirstOrDefaultAsync(x => x.Description.ClassId == asset.ClassId)
            );

            // Find the item asset description, or import it if missing
            var assetDescription = (dbItem?.Description ??
                await _steamDb.SteamAssetDescriptions
                    .Include(x => x.App)
                    .Include(x => x.CreatorProfile)
                    .FirstOrDefaultAsync(x => x.AppId == app.Id && x.ClassId == asset.ClassId)
            );
            if (assetDescription == null)
            {
                var importAssetDescription = await _commandProcessor.ProcessWithResultAsync(new ImportSteamAssetDescriptionRequest()
                {
                    AppId = ulong.Parse(app.SteamId),
                    AssetClassId = asset.ClassId
                });
                assetDescription = importAssetDescription.AssetDescription;
                if (assetDescription == null)
                {
                    // The asset description for this item doesn't exist, bail...
                    return null;
                }
            }

            // If the store item doesn't exist yet, create it now
            if (dbItem == null)
            {
                app.StoreItems.Add(dbItem = new SteamStoreItem()
                {
                    App = app,
                    AppId = app.Id,
                    Description = assetDescription,
                    DescriptionId = assetDescription.Id
                });

                var prices = ParseStoreItemPriceTable(asset.Prices);
                dbItem.UpdatePrice(
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
            dbItem.SteamId = asset.Name;
            dbItem.IsAvailable = true;
            return dbItem;
        }

        private IDictionary<string, long> ParseStoreItemPriceTable(AssetPricesModel prices)
        {
            return prices.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(
                    k => k.Name,
                    prop => (long)((uint)prop.GetValue(prices, null))
                );
        }

        private async Task<string> RegenerateStoreItemsThumbnailImage(SteamApp app, SteamItemStore store)
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
                _logger.LogError(ex, "Failed to generate store item thumbnail image");
            }

            return store.ItemsThumbnailUrl;
        }

        private async Task BroadcastStoreItemAddedMessages(SteamApp app, SteamItemStore store, IEnumerable<SteamStoreItemItemStore> newStoreItems, IEnumerable<SteamCurrency> currencies)
        {
            newStoreItems = newStoreItems?.OrderBy(x => x.Item?.Description?.Name);
            if (newStoreItems?.Any() != true)
            {
                return;
            }

            var defaultCurrency = currencies.FirstOrDefault(x => x.Name == Constants.SteamDefaultCurrency);
            var broadcastTasks = new List<Task>
        {
            _serviceBus.SendMessageAsync(new StoreAddedMessage()
            {
                AppId = UInt64.Parse(app.SteamId),
                AppName = app.Name,
                AppIconUrl = app.IconUrl,
                AppColour = app.PrimaryColor,
                StoreId = store.StoreId(),
                StoreName = store.StoreName(),
                Items = newStoreItems.Select(x => new StoreAddedMessage.Item()
                {
                    Name = x.Item.Description?.Name,
                    Currency = defaultCurrency.Name,
                    Price = x.Price,
                    PriceDescription = x.Price != null ? defaultCurrency?.ToPriceString(x.Price.Value) : null
                }).ToArray(),
                ItemsImageUrl = store.ItemsThumbnailUrl
            })
        };

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
                        ItemImageUrl = storeItem.Item.Description?.PreviewUrl ?? storeItem.Item.Description?.IconLargeUrl ?? storeItem.Item.Description?.IconUrl,
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

        private async Task AddNewMarketItemsFromArchive(SteamApp app, IEnumerable<ItemDefinition> itemDefinitions, ICollection<SteamAssetDescription> assetDescriptions, IEnumerable<SteamCurrency> currencies)
        {
            var marketItems = itemDefinitions
                .Join(assetDescriptions, x => x.ItemDefId, y => y.ItemDefinitionId, (x, y) => new
                {
                    ItemDefinition = x,
                    AssetDescription = y,
                    HasBecomeUnmarketable = (!x.Marketable && y.IsMarketable),
                    HasBecomeMarketable = (x.Marketable && !y.IsMarketable)
                })
                .ToArray();

            foreach (var newMarketableItem in marketItems.Where(x => x.HasBecomeMarketable && x.AssetDescription != null))
            {
                try
                {
                    var assetDescription = newMarketableItem.AssetDescription;
                    var marketItem = assetDescription.MarketItem;

                    // Double check that this asset description can currently be listed on the Steam community market
                    var marketPriceOverviewResponse = await _steamCommunityClient.GetMarketPriceOverview(new SteamMarketPriceOverviewJsonRequest()
                    {
                        AppId = assetDescription.App.SteamId,
                        MarketHashName = assetDescription.NameHash,
                        NoRender = true
                    });

                    if (marketPriceOverviewResponse?.Success != true)
                    {
                        continue;
                    }

                    // Add a new market item for this asset description
                    var defaultCurrency = currencies.FirstOrDefault(x => x.Name == Constants.SteamDefaultCurrency);
                    app.MarketItems.Add(assetDescription.MarketItem = marketItem = new SteamMarketItem()
                    {
                        SteamId = assetDescription.NameId?.ToString(),
                        AppId = app.Id,
                        App = app,
                        Description = assetDescription,
                        Currency = defaultCurrency,
                    });

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
                        ItemImageUrl = marketItem.Description?.PreviewUrl ?? marketItem.Description?.IconLargeUrl ?? marketItem.Description?.IconUrl,
                    });

                    // Reimport the asset description to import the Steam community market listing details (e.g. "name id")
                    if (assetDescription.ClassId > 0)
                    {
                        // TODO: Queue this in the background via service bus
                        await _steamDb.SaveChangesAsync();
                        var importedAssetDescription = await _commandProcessor.ProcessWithResultAsync(new ImportSteamAssetDescriptionRequest()
                        {
                            AppId = UInt64.Parse(app.SteamId),
                            AssetClassId = assetDescription.ClassId.Value
                        });
                    }
                }
                catch (SteamRequestException ex)
                {
                    if (ex.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                    {
                        // This means the item cannot be listed on the Steam community market yet, ignore for now...
                        _logger.LogWarning($"Item definition (id: {newMarketableItem.ItemDefinition.ItemDefId}, name: '{newMarketableItem.ItemDefinition.Name}') is marketable, but SCM is not allowing market listings yet");
                    }
                    else
                    {
                        throw;
                    }
                }

            }

            foreach (var newUnmarketableItem in marketItems.Where(x => x.HasBecomeUnmarketable && x.AssetDescription != null))
            {
                // TODO: Handle this...
            }
        }
    }
}
