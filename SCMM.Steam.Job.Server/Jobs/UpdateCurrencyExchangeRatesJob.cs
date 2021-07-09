using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Azure.ServiceBus;
using SCMM.Steam.API;
using SCMM.Steam.API.Messages;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.Community.Responses.Json;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Job.Server.Jobs.Cron;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Steam.Job.Server.Jobs
{
    public class UpdateCurrencyExchangeRatesJob : CronJobService
    {
        private readonly ILogger<UpdateCurrencyExchangeRatesJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public UpdateCurrencyExchangeRatesJob(IConfiguration configuration, ILogger<UpdateCurrencyExchangeRatesJob> logger, IServiceScopeFactory scopeFactory)
            : base(logger, configuration.GetJobConfiguration<UpdateCurrencyExchangeRatesJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var serviceBusClient = scope.ServiceProvider.GetService<ServiceBusClient>();
            var commnityClient = scope.ServiceProvider.GetService<SteamCommunityWebClient>();
            var steamService = scope.ServiceProvider.GetRequiredService<SteamService>();
            var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
            var timeChecked = DateTimeOffset.UtcNow;

            // TODO: Make this work
            /*
            var consolidationCutOff = DateTimeOffset.UtcNow.Date.Subtract(TimeSpan.FromDays(7));
            var ratesToConsolidate = await db.SteamCurrencyExchangeRates
                .GroupBy(x => new { x.CurrencyId, x.Timestamp })
                .Where(x => x.Key.Timestamp < consolidationCutOff)
                .Where(x => x.Count() > 1)
                .ToListAsync();

            foreach (var rateToConsolidate in ratesToConsolidate)
            {
                var consolidatedRate = rateToConsolidate.Average(x => x.ExchangeRateMultiplier);
                db.SteamCurrencyExchangeRates.RemoveRange(rateToConsolidate);
                db.SteamCurrencyExchangeRates.Add(new SteamCurrencyExchangeRate()
                {
                    CurrencyId = rateToConsolidate.Key.CurrencyId,
                    Timestamp = rateToConsolidate.Key.Timestamp.Date,
                    ExchangeRateMultiplier = consolidatedRate
                });

                db.SaveChanges();
            }
            */

            var mostExpensiveItem = db.SteamMarketItems
                .Include(x => x.App)
                .Include(x => x.Description)
                .OrderByDescending(x => x.BuyNowPrice)
                .FirstOrDefault();

            if (mostExpensiveItem == null)
            {
                return;
            }

            var language = db.SteamLanguages.FirstOrDefault(x => x.IsDefault);
            if (language == null)
            {
                return;
            }

            var currencies = db.SteamCurrencies.ToList();
            if (!currencies.Any())
            {
                return;
            }

            var systemCurrencyPrice = 0L;
            var systemCurrency = currencies.FirstOrDefault(x => x.IsDefault);
            if (systemCurrency == null)
            {
                return;
            }

            // Update exchange rates for currencies
            var currencyPrices = new Dictionary<SteamCurrency, SteamMarketPriceOverviewJsonResponse>();
            foreach (var currency in currencies.OrderByDescending(x => x.IsDefault))
            {
                var response = await commnityClient.GetMarketPriceOverview(
                    new SteamMarketPriceOverviewJsonRequest()
                    {
                        AppId = mostExpensiveItem.App.SteamId,
                        MarketHashName = mostExpensiveItem.Description.Name,
                        Language = language.SteamId,
                        CurrencyId = currency.SteamId,
                        NoRender = true
                    }
                );

                if (response?.Success != true)
                {
                    continue;
                }

                currencyPrices[currency] = response;
                var currencyPrice = response.LowestPrice.SteamPriceAsInt();
                if (currency == systemCurrency)
                {
                    systemCurrencyPrice = currencyPrice;
                }
                if (currencyPrice > 0 && systemCurrencyPrice > 0)
                {
                    currency.ExchangeRateMultiplier = ((decimal)currencyPrice / systemCurrencyPrice);
                }

                // TODO: Find a better way to deal with Steam's rate limiting.
                Thread.Sleep(3000);
            }

            // Add a historical record for each currency exchange rate change
            var basePrice = currencyPrices.FirstOrDefault(x => x.Key.Name == Constants.SteamDefaultCurrency);
            if (basePrice.Value?.Success == true)
            {
                foreach (var currencyPrice in currencyPrices)
                {
                    var baseLowestPrice = basePrice.Value.LowestPrice.SteamPriceAsInt();
                    var currencyLowestPrice = currencyPrice.Value.LowestPrice.SteamPriceAsInt();
                    var exchangeRateMultiplier = (baseLowestPrice > 0 && currencyLowestPrice > 0)
                        ? ((decimal)currencyLowestPrice / (decimal)baseLowestPrice)
                        : (0);

                    db.SteamCurrencyExchangeRates.Add(
                        new SteamCurrencyExchangeRate()
                        {
                            CurrencyId = currencyPrice.Key.Name,
                            Timestamp = timeChecked,
                            ExchangeRateMultiplier = exchangeRateMultiplier
                        }
                    );
                }
            }

            db.SaveChanges();

            var currencyExchangeRateUpdatedMessages = new List<CurrencyExchangeRateUpdateMessage>();
            foreach (var currencyPrice in currencyPrices)
            {
                currencyExchangeRateUpdatedMessages.Add(
                    new CurrencyExchangeRateUpdateMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Currency = currencyPrice.Key.Name,
                        ExchangeRateMultiplier = currencyPrice.Key.ExchangeRateMultiplier
                    }
                );
            }

            await serviceBusClient.SendMessagesAsync(currencyExchangeRateUpdatedMessages);
        }
    }
}
