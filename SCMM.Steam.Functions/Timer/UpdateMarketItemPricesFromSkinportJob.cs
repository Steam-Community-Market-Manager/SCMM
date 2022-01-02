using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.Skinport.Client;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemPricesFromSkinportJob
{
    private readonly SteamDbContext _db;
    private readonly SkinportWebClient _skinportWebClient;

    public UpdateMarketItemPricesFromSkinportJob(SteamDbContext db, SkinportWebClient skinportWebClient)
    {
        _db = db;
        _skinportWebClient = skinportWebClient;
    }

    [Function("Update-Market-Item-Prices-From-Skinport")]
    public async Task Run([TimerTrigger("0 33 * * * *")] /* every hour, 33 minutes after the hour */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Prices-From-Skinport");

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
                logger.LogTrace($"Updating market item price information from Skinport (appId: {app.SteamId})");
                var items = await _db.SteamMarketItems
                    .Select(x => new
                    {
                        Name = x.Description.NameHash,
                        Currency = x.Currency,
                        Item = x,
                    })
                    .ToListAsync();

                var skinportItems = await _skinportWebClient.GetItemsAsync(app.SteamId, currency: usdCurrency.Name);
                if (skinportItems?.Any() != true)
                {
                    continue;
                }

                foreach (var skinportItem in skinportItems)
                {
                    var item = items.FirstOrDefault(x => x.Name == skinportItem.MarketHashName)?.Item;
                    if (item != null)
                    {
                        item.Prices = new PersistablePriceStockDictionary(item.Prices);
                        item.Prices[PriceType.Skinport] = new PriceStock
                        {
                            Price = skinportItem.Quantity > 0 ? item.Currency.CalculateExchange((skinportItem.MinPrice ?? skinportItem.SuggestedPrice).ToString().SteamPriceAsInt(), usdCurrency) : 0,
                            Stock = skinportItem.Quantity
                        };
                        item.UpdateBuyNowPrice();
                    }
                }

                var missingItems = items.Where(x => !skinportItems.Any(y => x.Name == y.MarketHashName) && x.Item.Prices.ContainsKey(PriceType.Skinport));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.Prices = new PersistablePriceStockDictionary(missingItem.Item.Prices);
                    missingItem.Item.Prices.Remove(PriceType.Skinport);
                    missingItem.Item.UpdateBuyNowPrice();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item price information from Skinport (appId: {app.SteamId}). {ex.Message}");
                continue;
            }

            _db.SaveChanges();
        }
    }
}
