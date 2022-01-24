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
    public async Task Run([TimerTrigger("0 14-59/20 * * * *")] /* every 20mins */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Prices-From-TradeitGG");

        var steamApps = await _db.SteamApps.Where(x => x.IsActive).ToListAsync();
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
            logger.LogTrace($"Updating item price information from Tradeit.gg (appId: {app.SteamId})");
         
            try
            {
                var tradeitGGItems = new Dictionary<TradeitGGItem, int>();
                var inventoryDataItems = (IDictionary<TradeitGGItem, int>) null;
                var inventoryDataOffset = 0;
                do
                {
                    // NOTE: Items have to be fetched in multiple batches, keep reading until no new items are found
                    inventoryDataItems = await _tradeitGGWebClient.GetInventoryDataAsync(app.SteamId, offset: inventoryDataOffset, limit: TradeitGGWebClient.MaxPageLimit);
                    if (inventoryDataItems?.Any() == true)
                    {
                        tradeitGGItems.AddRange(inventoryDataItems);
                        inventoryDataOffset += TradeitGGWebClient.MaxPageLimit;
                    }
                } while (inventoryDataItems?.Any() == true);

                var items = await _db.SteamMarketItems
                    .Where(x => x.AppId == app.Id)
                    .Select(x => new
                    {
                        Name = x.Description.NameHash,
                        Currency = x.Currency,
                        Item = x,
                    })
                    .ToListAsync();

                foreach (var tradeitGGItem in tradeitGGItems)
                {
                    var item = items.FirstOrDefault(x => x.Name == tradeitGGItem.Key.Name)?.Item;
                    if (item != null)
                    {
                        item.UpdateBuyPrices(MarketType.TradeitGG, new PriceWithSupply
                        {
                            Price = tradeitGGItem.Value > 0 ? item.Currency.CalculateExchange(tradeitGGItem.Key.Price, usdCurrency) : 0,
                            Supply = tradeitGGItem.Value
                        });
                    }
                }

                var missingItems = items.Where(x => !tradeitGGItems.Any(y => x.Name == y.Key.Name) && x.Item.BuyPrices.ContainsKey(MarketType.TradeitGG));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.TradeitGG, null);
                }

                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item price information from Tradeit.gg (appId: {app.SteamId}). {ex.Message}");
                continue;
            }
        }
    }
}
