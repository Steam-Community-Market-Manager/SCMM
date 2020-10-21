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
                var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
                var discord = scope.ServiceProvider.GetRequiredService<DiscordClient>();
                var steamService = scope.ServiceProvider.GetRequiredService<SteamService>();
                var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_steamConfiguration.ApplicationKey);
                var steamEconomy = steamWebInterfaceFactory.CreateSteamWebInterface<SteamEconomy>();

                var steamApps = await db.SteamApps.ToListAsync();
                if (!steamApps.Any())
                {
                    return;
                }

                var currencies = await db.SteamCurrencies.Where(x => x.IsCommon).ToListAsync();
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

                    var newStoreItems = new List<SteamStoreItem>();
                    foreach (var asset in response.Data.Assets)
                    {
                        var storeItem = await steamService.AddOrUpdateAppStoreItem(
                            app, currency, language, asset, DateTimeOffset.Now
                        );
                        if (storeItem.IsTransient)
                        {
                            newStoreItems.Add(storeItem);
                        }
                    }

                    if (newStoreItems.Any())
                    {
                        var culture = CultureInfo.InvariantCulture;
                        var storeDate = DateTimeOffset.UtcNow.Date;
                        int storeDateWeek = culture.Calendar.GetWeekOfYear(storeDate, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday);
                        var storeTitle = $"{storeDate.ToString("yyyy")} Week {storeDateWeek}";
                        var latestStore = db.SteamItemStores
                            .Where(x => x.AppId == app.Id)
                            .GroupBy(x => 1)
                            .Select(x => x.Max(y => y.Start))
                            .FirstOrDefault();
                        var itemStore = db.SteamItemStores
                            .Where(x => x.Start == latestStore)
                            .Include(x => x.Items).ThenInclude(x => x.Item)
                            .Include(x => x.Items).ThenInclude(x => x.Item.Description)
                            .FirstOrDefault();

                        if (itemStore == null || itemStore.Start.Date != DateTimeOffset.UtcNow.Date)
                        {
                            if (itemStore != null)
                            {
                                itemStore.End = DateTimeOffset.UtcNow;
                            }
                            db.SteamItemStores.Add(
                                itemStore = new SteamItemStore()
                                {
                                    App = app,
                                    AppId = app.Id,
                                    Name = storeTitle,
                                    Start = DateTimeOffset.UtcNow
                                }
                            );
                        }

                        foreach(var item in newStoreItems)
                        {
                            if (!itemStore.Items.Any(x => x.Item.SteamId == item.SteamId))
                            {
                                itemStore.Items.Add(new SteamStoreItemItemStore()
                                {
                                    Store = itemStore,
                                    Item = item
                                });
                            }
                        }

                        db.SaveChanges();

                        await discord.BroadcastMessageAsync(
                            channelPattern: $"announcement|store|skin|{app.Name}",
                            message: null,
                            title: $"{app.Name} Store - {storeTitle}",
                            description: $"{newStoreItems.Count} new item(s) have been added to the {app.Name} store.",
                            fields: newStoreItems.OrderBy(x => x.Description.Name).ToDictionary(
                                x => x.Description?.Name,
                                x => GenerateStoreItemPriceList(x, currencies)
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
