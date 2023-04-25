using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.API.Events;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.Functions.Timer;

public class UpdateCurrencyExchangeRates
{
    private readonly SteamDbContext _db;
    private readonly AuthenticatedProxiedSteamCommunityWebClient _steamCommunityWebClient;
    private readonly IServiceBus _serviceBus;

    public UpdateCurrencyExchangeRates(SteamDbContext db, AuthenticatedProxiedSteamCommunityWebClient steamCommunityWebClient, IServiceBus serviceBus)
    {
        _db = db;
        _steamCommunityWebClient = steamCommunityWebClient;
        _serviceBus = serviceBus;
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
            .Where(x => x.App.IsActive)
            .Where(x => x.Description.IsMarketable)
            .Where(x => x.Description.IsCommodity)
            .Where(x => !String.IsNullOrEmpty(x.SteamId))
            .Where(x => x.SellOrderCount > 0 && x.SellOrderLowestPrice > 0)
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
            try
            {
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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error updating currency exchange rate for {currency.Name}");
                if (usdCurrency == currency)
                {
                    break; // USD price is required to calculate other currencies. If it fails, we can't continue
                }
                else
                {
                    continue;
                }
            }
        }

        _db.SaveChanges();

        // Let all other services know the exchange rates have changed (they may have them cached)
        var currencyExchangeRateUpdatedMessages = new List<CurrencyExchangeRateUpdatedMessage>();
        foreach (var currency in updatedCurrencies)
        {
            currencyExchangeRateUpdatedMessages.Add(
                new CurrencyExchangeRateUpdatedMessage()
                {
                    Timestamp = DateTime.UtcNow,
                    Currency = currency.Name,
                    ExchangeRateMultiplier = currency.ExchangeRateMultiplier
                }
            );
        }

        await _serviceBus.SendMessagesAsync(currencyExchangeRateUpdatedMessages);
    }
}
