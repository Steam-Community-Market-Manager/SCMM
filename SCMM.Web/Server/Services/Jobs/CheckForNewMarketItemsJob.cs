using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Discord.Client;
using SCMM.Steam.Client;
using SCMM.Steam.Shared.Community.Requests.Html;
using SCMM.Steam.Shared.Community.Requests.Json;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Data.Models.Steam;
using SCMM.Web.Server.Services.Jobs.CronJob;
using SCMM.Web.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs
{
    public class CheckForNewMarketItemsJob : CronJobService
    {
        private readonly ILogger<CheckForNewMarketItemsJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public CheckForNewMarketItemsJob(IConfiguration configuration, ILogger<CheckForNewMarketItemsJob> logger, IServiceScopeFactory scopeFactory)
            : base(logger, configuration.GetJobConfiguration<CheckForNewMarketItemsJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var discord = scope.ServiceProvider.GetRequiredService<DiscordClient>();
                var commnityClient = scope.ServiceProvider.GetService<SteamCommunityClient>();
                var steamService = scope.ServiceProvider.GetRequiredService<SteamService>();
                var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();

                var steamApps = db.SteamApps.ToList();
                if (!steamApps.Any())
                {
                    return;
                }

                var language = db.SteamLanguages.FirstOrDefault(x => x.IsDefault);
                if (language == null)
                {
                    return;
                }

                var currencies = await db.SteamCurrencies.Where(x => x.IsCommon).ToListAsync();
                if (currencies == null)
                {
                    return;
                }

                var currency = db.SteamCurrencies.FirstOrDefault(x => x.IsDefault);
                if (currency == null)
                {
                    return;
                }

                var pageRequests = new List<SteamMarketSearchPaginatedJsonRequest>();
                foreach (var app in steamApps)
                {
                    var appPageCountRequest = new SteamMarketSearchPaginatedJsonRequest()
                    {
                        AppId = app.SteamId,
                        Start = 1,
                        Count = 1,
                        Language = language.SteamId,
                        CurrencyId = currency.SteamId,
                        SortColumn = SteamMarketSearchPaginatedJsonRequest.SortColumnName
                    };

                    _logger.LogInformation($"Checking for new market items (appId: {app.SteamId})");
                    var appPageCountResponse = await commnityClient.GetMarketSearchPaginated(appPageCountRequest);
                    if (appPageCountResponse?.Success != true || appPageCountResponse?.TotalCount <= 0)
                    {
                        continue;
                    }

                    var total = appPageCountResponse.TotalCount;
                    var pageSize = SteamMarketSearchPaginatedJsonRequest.MaxPageSize;
                    var appPageRequests = new List<SteamMarketSearchPaginatedJsonRequest>();
                    for (var i = 0; i <= total; i += pageSize)
                    {
                        appPageRequests.Add(
                            new SteamMarketSearchPaginatedJsonRequest()
                            {
                                AppId = app.SteamId,
                                Start = i,
                                Count = Math.Min(total - i, pageSize),
                                Language = language.SteamId,
                                CurrencyId = currency.SteamId,
                                SortColumn = SteamMarketSearchPaginatedJsonRequest.SortColumnName
                            }
                        );
                    }

                    if (appPageRequests.Any())
                    {
                        pageRequests.AddRange(appPageRequests);
                    }
                }

                // Add a 10 second delay between requests to avoid "Too Many Requests" error
                var newMarketItems = await Observable.Interval(TimeSpan.FromSeconds(10))
                    .Zip(pageRequests, (x, y) => y)
                    .Select(x => Observable.FromAsync(() =>
                    {
                        _logger.LogInformation($"Checking for new market items (appId: {x.AppId}, start: {x.Start}, end: {x.Start + x.Count})");
                        return commnityClient.GetMarketSearchPaginated(x);
                    }))
                    .Merge()
                    .Where(x => x?.Success == true && x?.Results?.Count > 0)
                    .SelectMany(x =>
                    {
                        var tasks = steamService.FindOrAddSteamMarketItems(x.Results, currency);
                        Task.WaitAll(tasks);
                        return tasks.Result;
                    })
                    .Where(x => x?.IsTransient == true)
                    .Where(x => x?.App != null && x?.Description != null)
                    .ToList();

                if (newMarketItems.Any())
                {
                    foreach (var marketItem in newMarketItems)
                    {
                        var fields = new Dictionary<string, string>();
                        var storeItem = db.SteamStoreItems.FirstOrDefault(x => x.DescriptionId == marketItem.DescriptionId);
                        if (storeItem != null)
                        {
                            var estimatedSales = "Unknown";
                            if(storeItem.TotalSalesMax == null)
                            {
                                estimatedSales = $"{storeItem.TotalSalesMin.ToQuantityString()} or more";
                            }
                            else if (storeItem.TotalSalesMin == storeItem.TotalSalesMax)
                            {
                                estimatedSales = $"{storeItem.TotalSalesMin.ToQuantityString()}";
                            }
                            else
                            {
                                estimatedSales = $"{storeItem.TotalSalesMin.ToQuantityString()} - {storeItem.TotalSalesMax.Value.ToQuantityString()}";
                            }
                            fields.Add("Estimated Sales", estimatedSales);
                            fields.Add("Store Price", GenerateStoreItemPriceList(storeItem, currencies));
                        }
                        if (marketItem != null)
                        {
                            fields.Add("Market Price", GenerateMarketItemPriceList(marketItem, currencies));
                        }
                        await discord.BroadcastMessageAsync(
                            channelPattern: $"announcement|market|skin|{marketItem.App.Name}",
                            message: null,
                            title: $"{marketItem.Description.Name} is now available in the marketplace",
                            description: $"This item just appeared in the marketplace for the first time, or has reappeared after previously not having any listings.",
                            fields: fields,
                            url: new SteamMarketListingPageRequest()
                            {
                                AppId = marketItem.App.SteamId,
                                MarketHashName = marketItem.Description.Name
                            },
                            thumbnailUrl: marketItem.App.IconUrl,
                            imageUrl: marketItem.Description.IconUrl,
                            color: ColorTranslator.FromHtml(marketItem.App.PrimaryColor)
                        );
                    }

                    db.SaveChanges();
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

        private string GenerateMarketItemPriceList(SteamMarketItem marketItem, IEnumerable<SteamCurrency> currencies)
        {
            var prices = new List<String>();
            foreach (var currency in currencies.OrderBy(x => x.Name))
            {
                var priceString = currency.ToPriceString(currency.CalculateExchange(marketItem.BuyNowPrice, marketItem.Currency))?.Trim();
                if (!String.IsNullOrEmpty(priceString))
                {
                    prices.Add($"{currency.Name} {priceString}");
                }
            }

            return String.Join("  •  ", prices).Trim(' ', '•');
        }
    }
}
