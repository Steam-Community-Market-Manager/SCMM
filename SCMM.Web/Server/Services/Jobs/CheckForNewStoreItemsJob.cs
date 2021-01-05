using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Discord.Client;
using SCMM.Steam.Client;
using SCMM.Steam.Shared.Community.Requests.Html;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Data.Models.Steam;
using SCMM.Web.Server.Extensions;
using SCMM.Web.Server.Services.Jobs.CronJob;
using SCMM.Web.Shared;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs
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
                var db = scope.ServiceProvider.GetRequiredService<ScmmDbContext>();
                var discord = scope.ServiceProvider.GetRequiredService<DiscordClient>();
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
                        .Select(x => x.Items.Select(i => i.Item.SteamId))
                        .FirstOrDefault()
                        ?.OrderBy(x => x)
                        ?.ToList();

                    // If both stores contain the same items, then there is no need to update anything
                    var storesAreTheSame = (ourStoreItemIds != null && theirStoreItemIds.SequenceEqual(ourStoreItemIds));
                    if (storesAreTheSame)
                    {
                        continue;
                    }

                    // If we got here, then then item store has changed (either added or removed items)
                    // Load all of our active stores and their items
                    var activeItemStores = db.SteamItemStores
                        .Where(x => x.End == null)
                        .OrderByDescending(x => x.Start)
                        .Include(x => x.Items).ThenInclude(x => x.Item)
                        .Include(x => x.Items).ThenInclude(x => x.Item.Description)
                        .ToList();

                    // End any stores that have items no longer available
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

                    // Ensure that there is at least one active store (create a new one if needed)
                    var activeItemStore = activeItemStores.FirstOrDefault();
                    if (activeItemStore == null)
                    {
                        var culture = CultureInfo.InvariantCulture;
                        var storeDate = DateTimeOffset.UtcNow.Date;
                        int storeDateWeek = culture.Calendar.GetWeekOfYear(storeDate, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday);
                        var storeTitle = $"Week {storeDateWeek}";
                        db.SteamItemStores.Add(
                            activeItemStore = new SteamItemStore()
                            {
                                App = app,
                                AppId = app.Id,
                                Name = storeTitle,
                                Start = DateTimeOffset.UtcNow
                            }
                        );
                    }

                    // Ensure that the store items are available in the database (create them if missing)
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
                        if (!activeItemStore.Items.Any(x => x.Item.SteamId == item.SteamId))
                        {
                            newStoreItems.Add(item);
                            activeItemStore.Items.Add(new SteamStoreItemItemStore()
                            {
                                Store = activeItemStore,
                                Item = item
                            });
                        }
                    }

                    db.SaveChanges();
                    
                    // Send out a broadcast about any "new" items that weren't already in our store
                    await BroadcastNewStoreItemsNotification(discord, db, app, activeItemStore, newStoreItems, currencies);
                }
            }
        }

        private async Task BroadcastNewStoreItemsNotification(DiscordClient discord, ScmmDbContext db, SteamApp app, SteamItemStore store, IEnumerable<SteamStoreItem> newStoreItems, IEnumerable<SteamCurrency> currencies)
        {
            // NOTE: Delay for a bit to allow the database to flush before we generate the notification.
            //       This helps ensure that the mosaic image will generate correctly the first time.
            Thread.Sleep(10000);

            var guilds = db.DiscordGuilds.Include(x => x.Configurations).ToList();
            foreach (var guild in guilds)
            {
                if (guild.IsSet(Data.Models.Discord.DiscordConfiguration.Alerts) && !guild.Get(Data.Models.Discord.DiscordConfiguration.Alerts).Value.Contains(Data.Models.Discord.DiscordConfiguration.AlertsStore))
                {
                    continue;
                }

                var filteredCurrencies = currencies;
                var guildCurrencies = guild.List(Data.Models.Discord.DiscordConfiguration.Currency).Value;
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
                    channelPattern: guild.Get(Data.Models.Discord.DiscordConfiguration.AlertChannel, $"announcement|store|skin|{app.Name}").Value,
                    message: null,
                    title: $"{app.Name} Store - {store.Name}",
                    description: $"{newStoreItems.Count()} new item(s) have been added to the {app.Name} store.",
                    fields: newStoreItems.OrderBy(x => x.Description.Name).ToDictionary(
                        x => x.Description?.Name,
                        x => GenerateStoreItemPriceList(x, filteredCurrencies)
                    ),
                    url: new SteamItemStorePageRequest()
                    {
                        AppId = app.SteamId
                    },
                    thumbnailUrl: app.IconUrl,
                    imageUrl: $"{_configuration.GetBaseUrl()}/api/store/mosaic?timestamp={DateTime.UtcNow.Ticks}",
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
                        prices.Add($"{currency.Name} {priceString}");
                    }
                }
            }

            return String.Join("  •  ", prices).Trim(' ', '•');
        }
    }
}
