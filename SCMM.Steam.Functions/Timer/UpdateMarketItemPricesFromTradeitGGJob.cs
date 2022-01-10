using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.TradeitGG.Client;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemPricesFromTradeitGGJob
{
    private readonly SteamDbContext _db;
    private readonly TradeitGGWebClient _tradeitGGWebClient;

    public UpdateMarketItemPricesFromTradeitGGJob(SteamDbContext db, TradeitGGWebClient tradeitGGWebClient)
    {
        _db = db;
        _tradeitGGWebClient = tradeitGGWebClient;
    }

    [Function("Update-Market-Item-Prices-From-TradeitGG")]
    public async Task Run([TimerTrigger("0 5-59/15 * * * *")] /* every 15mins */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Prices-From-TradeitGG");

        var steamApps = await _db.SteamApps.ToListAsync();
        if (!steamApps.Any())
        {
            return;
        }

        // Prices are returned in USD by default
        var usdCurrency = _db.SteamCurrencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyUSD);
        if (usdCurrency == null)
        {
            return;
        }

        foreach (var app in steamApps)
        {
            try
            {
                logger.LogTrace($"Updating item price information from Tradeit.gg (appId: {app.SteamId})");
                var items = await _db.SteamMarketItems
                    .Select(x => new
                    {
                        Name = x.Description.NameHash,
                        Currency = x.Currency,
                        Item = x,
                    })
                    .ToListAsync();

                var tradeitGGItems = new Dictionary<TradeitGGItem, int>();
                var inventoryDataItems = (IDictionary<TradeitGGItem, int>) null;
                var inventoryDataOffset = 0;
                const int inventoryDataLimit = 200;
                do
                {
                    // NOTE: Items have to be fetched in multiple batches of 200, keep reading until no new items are found
                    inventoryDataItems = await _tradeitGGWebClient.GetInventoryDataAsync(app.SteamId, offset: inventoryDataOffset, limit: inventoryDataLimit);
                    if (inventoryDataItems?.Any() == true)
                    {
                        tradeitGGItems.AddRange(inventoryDataItems);
                        inventoryDataOffset += inventoryDataLimit;
                    }
                } while (inventoryDataItems?.Any() == true);
                if (tradeitGGItems?.Any() != true)
                {
                    continue;
                }

                foreach (var tradeitGGItem in tradeitGGItems)
                {
                    var item = items.FirstOrDefault(x => x.Name == tradeitGGItem.Key.Name)?.Item;
                    if (item != null)
                    {
                        // NOTE: Trade and store share the same item inventory, but buying from the store has a fixed discount
                        item.UpdateBuyPrices(MarketType.TradeitGGTrade, new PriceStock
                        {
                            Price = tradeitGGItem.Value > 0 ? item.Currency.CalculateExchange(tradeitGGItem.Key.Price, usdCurrency) : 0,
                            Stock = tradeitGGItem.Value
                        });
                        item.UpdateBuyPrices(MarketType.TradeitGGStore, new PriceStock
                        {
                            Price = tradeitGGItem.Value > 0 ? item.Currency.CalculateExchange(tradeitGGItem.Key.Price - (long)Math.Round(tradeitGGItem.Key.Price * TradeitGGWebClient.StoreDiscountMultiplier, 0), usdCurrency) : 0,
                            Stock = tradeitGGItem.Value
                        });
                    }
                }

                var missingItems = items.Where(x => !tradeitGGItems.Any(y => x.Name == y.Key.Name) && x.Item.BuyPrices.ContainsKey(MarketType.TradeitGGStore));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.TradeitGGTrade, null);
                    missingItem.Item.UpdateBuyPrices(MarketType.TradeitGGStore, null);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item price information from Tradeit.gg (appId: {app.SteamId}). {ex.Message}");
                continue;
            }

            _db.SaveChanges();
        }
    }
}
