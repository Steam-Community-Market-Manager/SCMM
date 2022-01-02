using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.SwapGG.Client;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemPricesFromSwapGGJob
{
    private readonly SteamDbContext _db;
    private readonly SwapGGWebClient _swapGGWebClient;

    public UpdateMarketItemPricesFromSwapGGJob(SteamDbContext db, SwapGGWebClient swapGGWebClient)
    {
        _db = db;
        _swapGGWebClient = swapGGWebClient;
    }

    [Function("Update-Market-Item-Prices-From-SwapGG")]
    public async Task Run([TimerTrigger("0 34 * * * *")] /* every hour, 34 minutes after the hour */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Prices-From-SwapGG");

        var steamApps = await _db.SteamApps.ToListAsync();
        if (!steamApps.Any())
        {
            return;
        }

        // Prices are returned in EUR by default
        var eurCurrency = _db.SteamCurrencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyEUR);
        if (eurCurrency == null)
        {
            return;
        }

        foreach (var app in steamApps)
        {
            try
            {
                logger.LogTrace($"Updating market item price information from swap.gg (appId: {app.SteamId})");
                var items = await _db.SteamMarketItems
                    .Select(x => new
                    {
                        Name = x.Description.NameHash,
                        Currency = x.Currency,
                        Item = x,
                    })
                    .ToListAsync();

                var swapggItems = await _swapGGWebClient.GetItemPricingLowestAsync(app.SteamId);
                if (swapggItems?.Any() != true)
                {
                    continue;
                }

                foreach (var swapggItem in swapggItems)
                {
                    var item = items.FirstOrDefault(x => x.Name == swapggItem.Key)?.Item;
                    if (item != null)
                    {
                        item.Prices = new PersistablePriceStockDictionary(item.Prices);
                        item.Prices[PriceType.SwapGG] = new PriceStock
                        {
                            Price = swapggItem.Value.Quantity > 0 ? item.Currency.CalculateExchange(swapggItem.Value.Price, eurCurrency) : 0,
                            Stock = swapggItem.Value.Quantity
                        };
                        item.UpdateBuyNowPrice();
                    }
                }

                var missingItems = items.Where(x => !swapggItems.Any(y => x.Name == y.Key) && x.Item.Prices.ContainsKey(PriceType.SwapGG));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.Prices = new PersistablePriceStockDictionary(missingItem.Item.Prices);
                    missingItem.Item.Prices.Remove(PriceType.SwapGG);
                    missingItem.Item.UpdateBuyNowPrice();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item price information from swap.gg (appId: {app.SteamId}). {ex.Message}");
                continue;
            }

            _db.SaveChanges();
        }
    }
}
