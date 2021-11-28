﻿using CommandQuery;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Discord.API.Commands;
using SCMM.Shared.API.Extensions;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store;
using SCMM.Steam.API;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;

namespace SCMM.Steam.Functions.Jobs;

public class CheckForNewStoreItemsJob
{
    private readonly IConfiguration _configuration;
    private readonly SteamDbContext _db;
    private readonly ICommandProcessor _commandProcessor;
    private readonly IQueryProcessor _queryProcessor;
    private readonly SteamCommunityWebClient _steamCommunityWebClient;
    private readonly SteamService _steamService;

    public CheckForNewStoreItemsJob(IConfiguration configuration, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, SteamDbContext db, SteamCommunityWebClient steamCommunityWebClient, SteamService steamService)
    {
        _configuration = configuration;
        _commandProcessor = commandProcessor;
        _queryProcessor = queryProcessor;
        _db = db;
        _steamCommunityWebClient = steamCommunityWebClient;
        _steamService = steamService;
    }

    [Function("Check-New-Store-Items")]
    public async Task Run([TimerTrigger("0 * * * * *")] /* every minute */ object timer, FunctionContext context)
    {
        var logger = context.GetLogger("Check-New-Store-Items");

        var steamApps = await _db.SteamApps.ToListAsync();
        if (!steamApps.Any())
        {
            return;
        }

        var currencies = await _db.SteamCurrencies.ToListAsync();
        if (currencies == null)
        {
            return;
        }

        var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_configuration.GetSteamConfiguration().ApplicationKey);
        var steamEconomy = steamWebInterfaceFactory.CreateSteamWebInterface<SteamEconomy>();
        foreach (var app in steamApps)
        {
            logger.LogInformation($"Checking for new store items (appId: {app.SteamId})");
            var usdCurrency = currencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyUSD);
            var response = await steamEconomy.GetAssetPricesAsync(
                uint.Parse(app.SteamId), string.Empty, Constants.SteamDefaultLanguage
            );
            if (response?.Data?.Success != true)
            {
                logger.LogError("Failed to get asset prices");
                continue;
            }

            // We want to compare the Steam item store with our most recent store

            var theirStoreItemIds = response.Data.Assets
                .Select(x => x.Name)
                .OrderBy(x => x)
                .ToList();
            var ourStoreItemIds = _db.SteamItemStores
                .Where(x => x.End == null)
                .OrderByDescending(x => x.Start)
                .SelectMany(x => x.Items.Select(i => i.Item.SteamId))
                .ToList();

            // If both stores contain the same items, then there is no need to update anything
            var storesAreTheSame = ourStoreItemIds != null && theirStoreItemIds.All(x => ourStoreItemIds.Contains(x)) && ourStoreItemIds.All(x => theirStoreItemIds.Contains(x));
            if (storesAreTheSame)
            {
                continue;
            }

            // If we got here, then then item store has changed (either added or removed items)
            // Load all of our active stores and their items
            var activeItemStores = _db.SteamItemStores
                .Include(x => x.ItemsThumbnail)
                .Where(x => x.End == null)
                .OrderByDescending(x => x.Start)
                .Include(x => x.Items).ThenInclude(x => x.Item)
                .Include(x => x.Items).ThenInclude(x => x.Item.Description)
                .ToList();

            // Check if there are any items or stores that are no longer available
            var generalItemsWereRemoved = false;
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
                            if (missingStoreItem.Item.Description.IsPermanent)
                            {
                                generalItemsWereRemoved = true;
                            }
                            else
                            {
                                limitedItemsWereRemoved = true;
                            }
                        }
                    }
                }
                if (itemStore.Start != null && itemStore.Items.All(x => !x.Item.IsAvailable))
                {
                    itemStore.End = DateTimeOffset.UtcNow;
                    activeItemStores.Remove(itemStore);
                }
            }

            // Ensure that an active "general" and "limited" item store exists
            var generalItemStore = activeItemStores.FirstOrDefault(x => x.Start == null);
            if (generalItemStore == null)
            {
                generalItemStore = new SteamItemStore()
                {
                    App = app,
                    AppId = app.Id,
                    Name = "General"
                };
            }
            var limitedItemStore = activeItemStores.FirstOrDefault(x => x.Start != null);
            if (limitedItemStore == null || limitedItemsWereRemoved)
            {
                limitedItemStore = new SteamItemStore()
                {
                    App = app,
                    AppId = app.Id,
                    Start = DateTimeOffset.UtcNow
                };
            }

            // Check if there are any new items to be added to the stores
            var newGeneralStoreItems = new List<SteamStoreItemItemStore>();
            var newLimitedStoreItems = new List<SteamStoreItemItemStore>();
            foreach (var asset in response.Data.Assets)
            {
                // Ensure that the item is available in the database (create them if missing)
                var storeItem = await _steamService.AddOrUpdateStoreItemAndMarkAsAvailable(
                    app, asset, DateTimeOffset.Now
                );
                if (storeItem == null)
                {
                    continue;
                }

                var itemStore = storeItem.Description.IsPermanent ? generalItemStore : limitedItemStore;
                if (itemStore == null)
                {
                    continue;
                }

                // Ensure that the item is linked to the store
                if (!storeItem.Stores.Any(x => x.StoreId == itemStore.Id))
                {
                    var prices = _steamService.ParseStoreItemPriceTable(asset.Prices);
                    var storeItemLink = new SteamStoreItemItemStore()
                    {
                        Store = itemStore,
                        Item = storeItem,
                        Currency = usdCurrency,
                        Price = prices.FirstOrDefault(x => x.Key == usdCurrency.Name).Value,
                        Prices = new PersistablePriceDictionary(prices),
                        IsPriceVerified = true
                    };
                    storeItem.Stores.Add(storeItemLink);
                    itemStore.Items.Add(storeItemLink);
                    if (storeItem.Description.IsPermanent)
                    {
                        newGeneralStoreItems.Add(storeItemLink);
                    }
                    else
                    {
                        newLimitedStoreItems.Add(storeItemLink);
                    }
                }

                // Update the store items "latest price"
                storeItem.UpdateLatestPrice();
            }

            // Regenerate store thumbnails (if items have changed)
            if (newGeneralStoreItems.Any())
            {
                if (generalItemStore.IsTransient)
                {
                    _db.SteamItemStores.Add(generalItemStore);
                }
                if (generalItemStore.ItemsThumbnail != null)
                {
                    _db.FileData.Remove(generalItemStore.ItemsThumbnail);
                    generalItemStore.ItemsThumbnail = null;
                    generalItemStore.ItemsThumbnailId = null;
                }
                var thumbnail = await GenerateStoreItemsThumbnailImage(_queryProcessor, generalItemStore.Items.Select(x => x.Item));
                if (thumbnail != null)
                {
                    generalItemStore.ItemsThumbnail = thumbnail;
                }
            }
            if (newLimitedStoreItems.Any())
            {
                if (limitedItemStore.IsTransient)
                {
                    _db.SteamItemStores.Add(limitedItemStore);
                }
                if (limitedItemStore.ItemsThumbnail != null)
                {
                    _db.FileData.Remove(limitedItemStore.ItemsThumbnail);
                    limitedItemStore.ItemsThumbnail = null;
                    limitedItemStore.ItemsThumbnailId = null;
                }
                var thumbnail = await GenerateStoreItemsThumbnailImage(_queryProcessor, limitedItemStore.Items.Select(x => x.Item));
                if (thumbnail != null)
                {
                    limitedItemStore.ItemsThumbnail = thumbnail;
                }
            }

            _db.SaveChanges();

            // Send out a broadcast about any "new" items that weren't already in our store
            if (newGeneralStoreItems.Any())
            {
                await BroadcastNewStoreItemsNotification(_commandProcessor, _db, app, generalItemStore, newGeneralStoreItems, currencies);
            }
            if (newLimitedStoreItems.Any())
            {
                await BroadcastNewStoreItemsNotification(_commandProcessor, _db, app, limitedItemStore, newLimitedStoreItems, currencies);
            }

            // TODO: Wait 1min, then trigger CheckForNewMarketItemsJob
        }
    }

    private async Task<FileData> GenerateStoreItemsThumbnailImage(IQueryProcessor queryProcessor, IEnumerable<SteamStoreItem> storeItems)
    {
        // Generate store thumbnail
        var items = storeItems.OrderBy(x => x.Description?.Name);
        var itemImageSources = items
            .Where(x => x.Description != null)
            .Select(x => new ImageSource()
            {
                Title = x.Description.Name,
                ImageUrl = x.Description.IconUrl,
                ImageData = x.Description.Icon?.Data,
            })
            .ToList();

        var thumbnail = await queryProcessor.ProcessAsync(new GetImageMosaicRequest()
        {
            ImageSources = itemImageSources,
            TileSize = 256,
            Columns = 3
        });

        if (thumbnail == null)
        {
            return null;
        }

        return new FileData()
        {
            MimeType = thumbnail.MimeType,
            Data = thumbnail.Data
        };
    }

    private async Task BroadcastNewStoreItemsNotification(ICommandProcessor commandProcessor, SteamDbContext db, SteamApp app, SteamItemStore store, IEnumerable<SteamStoreItemItemStore> newStoreItems, IEnumerable<SteamCurrency> currencies)
    {
        newStoreItems = newStoreItems?.OrderBy(x => x.Item?.Description?.Name);
        var guilds = db.DiscordGuilds.Include(x => x.Configurations).ToList();
        foreach (var guild in guilds)
        {
            if (guild.IsSet(DiscordConfiguration.Alerts) && !guild.Get(DiscordConfiguration.Alerts).Value.Contains(DiscordConfiguration.AlertsStore))
            {
                continue;
            }

            var guildChannels = guild.List(DiscordConfiguration.AlertChannel).Value?.Union(new[] {
                    "announcement", "store", "skin", app.Name, "general", "chat", "bot"
                });

            var filteredCurrencies = currencies;
            var guildCurrencies = guild.List(DiscordConfiguration.Currency).Value;
            if (guildCurrencies?.Any() == true)
            {
                filteredCurrencies = currencies.Where(x => guildCurrencies.Contains(x.Name)).ToList();
            }
            else
            {
                filteredCurrencies = currencies.Where(x => x.Name == Constants.SteamCurrencyUSD).ToList();
            }

            var storeId = store.Start != null
                ? store.Start.Value.UtcDateTime.AddMinutes(1).ToString(Constants.SCMMStoreIdDateFormat)
                : store.Name.ToLower();

            var storeName = store.Start != null
                ? $"{store.Start.Value.ToString("yyyy MMMM d")}{store.Start.Value.GetDaySuffix()}"
                : store.Name;

            await commandProcessor.ProcessAsync(new SendDiscordMessageRequest()
            {
                GuidId = ulong.Parse(guild.DiscordId),
                ChannelPatterns = guildChannels?.ToArray(),
                Message = null,
                Title = $"{app.Name} Store - {storeName}",
                Description = $"{newStoreItems.Count()} new item(s) have been added to the store.",
                Fields = newStoreItems.ToDictionary(
                    x => x.Item?.Description?.Name,
                    x => GenerateStoreItemPriceList(x, filteredCurrencies)
                ),
                FieldsInline = true,
                Url = $"{_configuration.GetWebsiteUrl()}/store/{storeId}",
                ThumbnailUrl = app.IconUrl,
                ImageUrl = $"{_configuration.GetWebsiteUrl()}/api/image/{store.ItemsThumbnailId}",
                Colour = app.PrimaryColor
            });
        }
    }

    private string GenerateStoreItemPriceList(SteamStoreItemItemStore storeItem, IEnumerable<SteamCurrency> currencies)
    {
        var prices = new List<string>();
        foreach (var currency in currencies.OrderBy(x => x.Name))
        {
            var price = storeItem.Prices.FirstOrDefault(x => x.Key == currency.Name);
            if (price.Value > 0)
            {
                var priceString = currency.ToPriceString(price.Value)?.Trim();
                if (!string.IsNullOrEmpty(priceString))
                {
                    prices.Add(priceString);
                }
            }
        }

        return string.Join("  •  ", prices).Trim(' ', '•');
    }
}