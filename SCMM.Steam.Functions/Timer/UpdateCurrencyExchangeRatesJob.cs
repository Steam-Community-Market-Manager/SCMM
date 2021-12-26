using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Azure.ServiceBus;
using SCMM.Steam.API.Messages;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.Functions.Timer;

public class UpdateCurrencyExchangeRatesJob
{
    private readonly SteamDbContext _db;
    private readonly SteamCommunityWebClient _steamCommunityWebClient;
    private readonly ServiceBusClient _serviceBusClient;

    public UpdateCurrencyExchangeRatesJob(SteamDbContext db, SteamCommunityWebClient steamCommunityWebClient, ServiceBusClient serviceBusClient)
    {
        _db = db;
        _steamCommunityWebClient = steamCommunityWebClient;
        _serviceBusClient = serviceBusClient;
    }

    [Function("Update-Currency-Exchange-Rates")]
    public async Task Run([TimerTrigger("0 55 * * * *")] /* every hour, 5 minutes before the hour */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Currency-Exchange-Rates");

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

        var mostExpensiveItem = _db.SteamMarketItems
            .Include(x => x.App)
            .Include(x => x.Description)
            .OrderByDescending(x => x.SellOrderLowestPrice)
            .FirstOrDefault();

        if (mostExpensiveItem == null)
        {
            return;
        }

        var currencies = _db.SteamCurrencies.ToList();
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

            var response = await _steamCommunityWebClient.GetMarketPriceOverview(
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
                logger.LogWarning($"Currency exchange rate for {currency.Name} could not be fetched");
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
                currencyExchangeRateMultiplier = (decimal)currencyPrice / usdPrice;
            }
            if (currencyExchangeRateMultiplier <= 0)
            {
                logger.LogWarning($"Currency exchange rate for {currency.Name} could not be calculated");
                continue;
            }

            // Set the exchange rate multiplier and add a historical exchange rate records (for historical conversions)
            currency.ExchangeRateMultiplier = currencyExchangeRateMultiplier;
            _db.SteamCurrencyExchangeRates.Add(
                new SteamCurrencyExchangeRate()
                {
                    CurrencyId = currency.Name,
                    Timestamp = timeChecked,
                    ExchangeRateMultiplier = currencyExchangeRateMultiplier
                }
            );

            updatedCurrencies.Add(currency);
        }

        _db.SaveChanges();

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

        await _serviceBusClient.SendMessagesAsync(currencyExchangeRateUpdatedMessages);
    }
}
