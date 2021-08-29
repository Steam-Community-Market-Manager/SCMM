using Microsoft.EntityFrameworkCore;
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

            var currencies = db.SteamCurrencies.ToList();
            if (!currencies.Any())
            {
                return;
            }

            var usdPrice = 0L;
            var usdCurrency = currencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyUSD);
            if (usdCurrency == null)
            {
                return;
            }

            // Update current exchange rates for each currency
            var updatedCurrencies = new List<SteamCurrency>();
            foreach (var currency in currencies.OrderByDescending(x => x.Name == usdCurrency.Name))
            {
                // TODO: Find a better way to deal with Steam's rate limiting.
                Thread.Sleep(3000);

                var response = await commnityClient.GetMarketPriceOverview(
                    new SteamMarketPriceOverviewJsonRequest()
                    {
                        AppId = mostExpensiveItem.App.SteamId,
                        MarketHashName = mostExpensiveItem.Description.Name,
                        Language = Constants.SteamDefaultLanguage,
                        CurrencyId = currency.SteamId,
                        NoRender = true
                    }
                );

                if (response?.Success != true)
                {
                    _logger.LogWarning($"Currency exchange rate for {currency.Name} could not be fetched");
                    continue;
                }

                var currencyPrice = response.LowestPrice.SteamPriceAsInt();
                if (usdCurrency == currency)
                {
                    usdPrice = currencyPrice;
                }
                var currencyExchangeRateMultiplier = 0m;
                if (usdPrice > 0 && currencyPrice > 0)
                {
                    currencyExchangeRateMultiplier = ((decimal)currencyPrice / usdPrice);
                }
                if (currencyExchangeRateMultiplier <= 0)
                {
                    _logger.LogWarning($"Currency exchange rate for {currency.Name} could not be calculated");
                    continue;
                }

                // Set the exchange rate multiplier and add a historical exchange rate records (for historical conversions)
                currency.ExchangeRateMultiplier = currencyExchangeRateMultiplier;
                db.SteamCurrencyExchangeRates.Add(
                    new SteamCurrencyExchangeRate()
                    {
                        CurrencyId = currency.Name,
                        Timestamp = timeChecked,
                        ExchangeRateMultiplier = currencyExchangeRateMultiplier
                    }
                );

                updatedCurrencies.Add(currency);
            }

            db.SaveChanges();

            // Let all other services know the exchange rates have changed (they may have them cached)
            var currencyExchangeRateUpdatedMessages = new List<CurrencyExchangeRateUpdateMessage>();
            foreach (var currency in updatedCurrencies)
            {
                currencyExchangeRateUpdatedMessages.Add(
                    new CurrencyExchangeRateUpdateMessage()
                    {
                        Timestamp = DateTime.UtcNow,
                        Currency = currency.Name,
                        ExchangeRateMultiplier = currency.ExchangeRateMultiplier
                    }
                );
            }

            await serviceBusClient.SendMessagesAsync(currencyExchangeRateUpdatedMessages);
        }
    }
}
