using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Client.Commands;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Store;

namespace SCMM.Discord.Bot.Server.Modules
{
    public partial class AdministrationModule
    {
        [Command("import-currency-exchange-rates")]
        public async Task<RuntimeResult> RebuildCurrencyExchangeRatesAsync()
        {
            var message = await Context.Message.ReplyAsync("Importing missing currency exchange rates...");
            var currencies = await _steamDb.SteamCurrencies.ToListAsync();
            var usdCurrency = currencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyUSD);

            var requiredCurrencyNames = currencies.Select(x => x.Name).ToArray();
            var requiredDates = await _steamDb.SteamMarketItemSale
                .Select(x => x.Timestamp.Date)
                .Distinct()
                .ToListAsync();
            var existingDates = await _steamDb.SteamCurrencyExchangeRates
                .GroupBy(x => x.Timestamp.Date)
                .Where(x => requiredCurrencyNames.Any(y => !x.Any(z => y == z.CurrencyId)))
                .Select(x => new
                {
                    Date = x.Key,
                    Currencies = x.Select(x => x.CurrencyId).Distinct()
                })
                .ToListAsync();

            var missingDates = requiredDates
                .Where(x => existingDates.Any(y => x.Date == y.Date))
                .OrderBy(x => x)
                .ToArray();

            // Import missing dates
            foreach (var batch in missingDates.Batch(100))
            {
                foreach (var missingDate in batch)
                {
                    await message.ModifyAsync(
                       x => x.Content = $"Importing all exchange rates for {missingDate.ToString("yyyy-MM-dd")} ({Array.IndexOf(missingDates, missingDate) + 1}/{missingDates.Length})..."
                    );

                    var exchangeRates = await _currencyExchangeService.GetHistoricalExchangeRatesAsync(missingDate, usdCurrency.Name, requiredCurrencyNames);
                    if (exchangeRates != null)
                    {
                        foreach (var exchangeRate in exchangeRates)
                        {
                            _steamDb.SteamCurrencyExchangeRates.Add(
                                new SteamCurrencyExchangeRate()
                                {
                                    CurrencyId = exchangeRate.Key,
                                    Timestamp = new DateTimeOffset(missingDate, TimeZoneInfo.Utc.BaseUtcOffset),
                                    ExchangeRateMultiplier = exchangeRate.Value
                                }
                            );
                        }
                    }
                }

                await _steamDb.SaveChangesAsync();
            }

            var missingRates = existingDates
                .Where(x => requiredCurrencyNames.Any(y => !x.Currencies.Any(z => y == z)))
                .OrderBy(x => x)
                .ToArray();

            // Import partially missing currencies
            foreach (var batch in missingRates.Batch(100))
            {
                foreach (var missingRate in batch)
                {
                    await message.ModifyAsync(
                       x => x.Content = $"Importing missing exchange rates for {missingRate.Date.ToString("yyyy-MM-dd")} ({Array.IndexOf(missingDates, missingRate) + 1}/{missingDates.Length})..."
                    );

                    var missingCurrencies = requiredCurrencyNames.Where(x => !missingRate.Currencies.Any(y => x == y)).ToArray();
                    var exchangeRates = await _currencyExchangeService.GetHistoricalExchangeRatesAsync(missingRate.Date, usdCurrency.Name, missingCurrencies);
                    if (exchangeRates != null)
                    {
                        foreach (var exchangeRate in exchangeRates)
                        {
                            _steamDb.SteamCurrencyExchangeRates.Add(
                                new SteamCurrencyExchangeRate()
                                {
                                    CurrencyId = exchangeRate.Key,
                                    Timestamp = new DateTimeOffset(missingRate.Date, TimeZoneInfo.Utc.BaseUtcOffset),
                                    ExchangeRateMultiplier = exchangeRate.Value
                                }
                            );
                        }
                    }
                }

                await _steamDb.SaveChangesAsync();
            }

            await message.ModifyAsync(
                x => x.Content = $"Imported {missingDates.Length} missing dates and {missingRates.Length} partially missing currency exchange rates"
            );

            return CommandResult.Success();
        }
    }
}
