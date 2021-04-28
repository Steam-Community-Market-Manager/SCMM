﻿using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Data.Shared;
using SCMM.Data.Shared.Extensions;
using SCMM.Data.Shared.Store;
using SCMM.Discord.Client;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Store;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SCMM.Web.Server.Jobs.CronJob;
using SCMM.Steam.API.Queries;
using SCMM.Steam.API;

namespace SCMM.Web.Server.Jobs
{
    public class CheckForNewStoreItemsJob : CronJobService
    {
        private readonly ILogger<CheckForNewStoreItemsJob> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SteamConfiguration _steamConfiguration;

        public CheckForNewStoreItemsJob(IConfiguration configuration, ILogger<CheckForNewStoreItemsJob> logger, IServiceScopeFactory scopeFactory)
            : base(logger, configuration.GetJobConfiguration<CheckForNewStoreItemsJob>())
        {
            _logger = logger;
            _configuration = configuration;
            _scopeFactory = scopeFactory;
            _steamConfiguration = configuration.GetSteamConfiguration();
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
                var discord = scope.ServiceProvider.GetRequiredService<DiscordClient>();
                var queryProcessor = scope.ServiceProvider.GetRequiredService<IQueryProcessor>();
                var steamService = scope.ServiceProvider.GetRequiredService<SteamService>();
                var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_steamConfiguration.ApplicationKey);
                var steamEconomy = steamWebInterfaceFactory.CreateSteamWebInterface<SteamEconomy>();

                var steamApps = await db.SteamApps.ToListAsync();
                if (!steamApps.Any())
                {
                    return;
                }

                var currencies = await db.SteamCurrencies.ToListAsync();
                if (currencies == null)
                {
                    return;
                }

                var language = await db.SteamLanguages.FirstOrDefaultAsync(x => x.IsDefault);
                if (language == null)
                {
                    return;
                }

                foreach (var app in steamApps)
                {
                    _logger.LogInformation($"Checking for new store items (appId: {app.SteamId})");
                    var currency = currencies.FirstOrDefault(x => x.IsDefault);
                    var response = await steamEconomy.GetAssetPricesAsync(
                        UInt32.Parse(app.SteamId), String.Empty, language.SteamId
                    );
                    if (response?.Data?.Success != true)
                    {
                        _logger.LogError("Failed to get asset prices");
                        continue;
                    }

                    // We want to compare the Steam item store with our most recent store
                    var theirStoreItemIds = response.Data.Assets
                        .Select(x => x.Name)
                        .OrderBy(x => x)
                        .ToList();
                    var ourStoreItemIds = db.SteamItemStores
                        .Where(x => x.End == null)
                        .OrderByDescending(x => x.Start)
                        .SelectMany(x => x.Items.Select(i => i.Item.SteamId))
                        .ToList();

                    // If both stores contain the same items, then there is no need to update anything
                    var storesAreTheSame = (ourStoreItemIds != null && theirStoreItemIds.All(x => ourStoreItemIds.Contains(x)));
                    if (storesAreTheSame)
                    {
                        continue;
                    }

                    // If we got here, then then item store has changed (either added or removed items)
                    // Load all of our active stores and their items
                    var activeItemStores = db.SteamItemStores
                        .Include(x => x.ItemsThumbnail)
                        .Where(x => x.End == null)
                        .OrderByDescending(x => x.Start)
                        .Include(x => x.Items).ThenInclude(x => x.Item)
                        .Include(x => x.Items).ThenInclude(x => x.Item.Description)
                        .ToList();

                    // End any stores that have items which are no longer available
                    foreach (var itemStore in activeItemStores.ToList())
                    {
                        var thisStoreItemIds = itemStore.Items.Select(x => x.Item.SteamId).ToList();
                        if (thisStoreItemIds.Any(x => !theirStoreItemIds.Contains(x)))
                        {
                            // TODO: We should be able to "end" individual items, rather than the entire store
                            itemStore.End = DateTimeOffset.UtcNow;
                            activeItemStores.Remove(itemStore);
                        }
                    }

                    // If there are no active item stores or they are older than 24hrs, create a new store
                    var newItemStore = activeItemStores.FirstOrDefault();
                    if (newItemStore == null || (DateTimeOffset.UtcNow - newItemStore.Start) > TimeSpan.FromDays(1))
                    {
                        var culture = CultureInfo.InvariantCulture;
                        var storeDate = DateTimeOffset.UtcNow.Date;
                        int storeDateWeek = culture.Calendar.GetWeekOfYear(storeDate, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday);
                        var storeTitle = $"Week {storeDateWeek}";
                        db.SteamItemStores.Add(
                            newItemStore = new SteamItemStore()
                            {
                                App = app,
                                AppId = app.Id,
                                Name = storeTitle,
                                Start = DateTimeOffset.UtcNow
                            }
                        );
                    }

                    // Ensure that all store items are available in the database (create them if missing)
                    var activeStoreItems = new List<SteamStoreItem>();
                    foreach (var asset in response.Data.Assets)
                    {
                        activeStoreItems.Add(
                            await steamService.AddOrUpdateAppStoreItem(
                                app, currency, language.SteamId, asset, DateTimeOffset.Now
                            )
                        );
                    }

                    // Ensure that the store items are linked to the active store
                    var newStoreItems = new List<SteamStoreItem>();
                    foreach (var item in activeStoreItems)
                    {
                        if (!item.Stores.Any(x => activeItemStores.Select(x => x.Id).Contains(x.StoreId)))
                        {
                            newStoreItems.Add(item);
                            newItemStore.Items.Add(new SteamStoreItemItemStore()
                            {
                                Store = newItemStore,
                                Item = item
                            });
                        }
                    }

                    // Regenerate the store items thumbnail
                    if (newItemStore.ItemsThumbnail == null)
                    {
                        var thumbnail = await GenerateStoreItemsThumbnail(queryProcessor, newItemStore.Items.Select(x => x.Item));
                        if (thumbnail != null)
                        {
                            newItemStore.ItemsThumbnail = thumbnail;
                        }
                    }

                    db.SaveChanges();

                    // Send out a broadcast about any "new" items that weren't already in our store
                    await BroadcastNewStoreItemsNotification(discord, db, app, newItemStore, newStoreItems, currencies);
                }
            }
        }

        private async Task<ImageData> GenerateStoreItemsThumbnail(IQueryProcessor queryProcessor, IEnumerable<SteamStoreItem> storeItems)
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

            return new ImageData()
            {
                Data = thumbnail.Data,
                MimeType = thumbnail.MimeType
            };
        }

        private async Task BroadcastNewStoreItemsNotification(DiscordClient discord, SteamDbContext db, SteamApp app, SteamItemStore store, IEnumerable<SteamStoreItem> newStoreItems, IEnumerable<SteamCurrency> currencies)
        {
            newStoreItems = newStoreItems?.OrderBy(x => x.Description.Name);
            var guilds = db.DiscordGuilds.Include(x => x.Configurations).ToList();
            foreach (var guild in guilds)
            {
                if (guild.IsSet(Steam.Data.Store.DiscordConfiguration.Alerts) && !guild.Get(Steam.Data.Store.DiscordConfiguration.Alerts).Value.Contains(Steam.Data.Store.DiscordConfiguration.AlertsStore))
                {
                    continue;
                }

                var filteredCurrencies = currencies;
                var guildCurrencies = guild.List(Steam.Data.Store.DiscordConfiguration.Currency).Value;
                if (guildCurrencies?.Any() == true)
                {
                    filteredCurrencies = currencies.Where(x => guildCurrencies.Contains(x.Name)).ToList();
                }
                else
                {
                    filteredCurrencies = currencies.Where(x => x.IsCommon).ToList();
                }

                await discord.BroadcastMessageAsync(
                    guildPattern: guild.DiscordId,
                    channelPattern: guild.Get(Steam.Data.Store.DiscordConfiguration.AlertChannel, $"announcement|store|skin|{app.Name}").Value,
                    message: null,
                    title: $"{app.Name} Store - {store.Name}",
                    description: $"{newStoreItems.Count()} new item(s) have been added to the {app.Name} store.",
                    fields: newStoreItems.ToDictionary(
                        x => x.Description?.Name,
                        x => GenerateStoreItemPriceList(x, filteredCurrencies)
                    ),
                    fieldsInline: true,
                    url: $"{_configuration.GetWebsiteUrl()}/steam/store",
                    thumbnailUrl: app.IconUrl,
                    imageUrl: $"{_configuration.GetWebsiteUrl()}/api/image/{store.ItemsThumbnailId}",
                    color: ColorTranslator.FromHtml(app.PrimaryColor)
                );
            }
        }

        private string GenerateStoreItemPriceList(SteamStoreItem storeItem, IEnumerable<SteamCurrency> currencies)
        {
            var prices = new List<String>();
            foreach (var currency in currencies.OrderBy(x => x.Name))
            {
                var price = storeItem.Prices.FirstOrDefault(x => x.Key == currency.Name);
                if (price.Value > 0)
                {
                    var priceString = currency.ToPriceString(price.Value)?.Trim();
                    if (!String.IsNullOrEmpty(priceString))
                    {
                        prices.Add(priceString);
                    }
                }
            }

            return String.Join("  •  ", prices).Trim(' ', '•');
        }
    }
}