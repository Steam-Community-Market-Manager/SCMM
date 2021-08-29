using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Client;
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
            var currencies = await _db.SteamCurrencies.ToListAsync();
            var usdCurrency = currencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyUSD);

            var requiredTimestamps = await _db.SteamMarketItemSale
                .GroupBy(x => x.Timestamp.Date)
                .Select(x => x.Key)
                .ToListAsync();
            var existingTimestamps = await _db.SteamCurrencyExchangeRates
                .GroupBy(x => x.Timestamp.Date)
                .Select(x => x.Key)
                .ToListAsync();

            var message = await Context.Message.ReplyAsync("Importing currency exchange rates...");
            var missingTimestamps = requiredTimestamps.Except(existingTimestamps).OrderBy(x => x).ToArray();
            foreach (var batch in missingTimestamps.Batch(100))
            {
                foreach (var timestamp in batch)
                {
                    await message.ModifyAsync(
                       x => x.Content = $"Importing exchange rates for {timestamp.ToString("yyyy-MM-dd")} ({Array.IndexOf(missingTimestamps, timestamp) + 1}/{missingTimestamps.Length})..."
                    );

                    var exchangeRates = await _fixerWebClient.GetHistoricalRatesAsync(timestamp, usdCurrency.Name, currencies.Select(x => x.Name).ToArray());
                    if (exchangeRates != null)
                    {
                        foreach (var exchangeRate in exchangeRates)
                        {
                            _db.SteamCurrencyExchangeRates.Add(
                                new SteamCurrencyExchangeRate()
                                {
                                    CurrencyId = exchangeRate.Key,
                                    Timestamp = new DateTimeOffset(timestamp, TimeZoneInfo.Utc.BaseUtcOffset),
                                    ExchangeRateMultiplier = exchangeRate.Value
                                }
                            );
                        }
                    }
                }

                await _db.SaveChangesAsync();
            }

            await message.ModifyAsync(
                x => x.Content = $"Imported {missingTimestamps.Length}/{missingTimestamps.Length} currency exchange rates"
            );

            return CommandResult.Success();
        }
    }
}
