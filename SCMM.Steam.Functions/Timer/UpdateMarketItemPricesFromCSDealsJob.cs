using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.CSDeals.Client;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemPricesFromCSDealsob
{
    private readonly SteamDbContext _db;
    private readonly CSDealsWebClient _csDealsWebClient;

    public UpdateMarketItemPricesFromCSDealsob(SteamDbContext db, CSDealsWebClient csDealsWebClient)
    {
        _db = db;
        _csDealsWebClient = csDealsWebClient;
    }

    [Function("Update-Market-Item-Prices-From-CSDeals")]
    public async Task Run([TimerTrigger("0 1-59/15 * * * *")] /* every 15mins */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Prices-From-CSDeals");

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
                logger.LogTrace($"Updating market item price information from CS.Deals (appId: {app.SteamId})");
                var items = await _db.SteamMarketItems
                    .Select(x => new
                    {
                        Name = x.Description.NameHash,
                        Currency = x.Currency,
                        Item = x,
                    })
                    .ToListAsync();

                var csDealsItems = await _csDealsWebClient.PricingGetLowestPricesAsync(app.SteamId);
                if (csDealsItems?.Any() != true)
                {
                    continue;
                }

                foreach (var csDealsItem in csDealsItems)
                {
                    var item = items.FirstOrDefault(x => x.Name == csDealsItem.MarketName)?.Item;
                    if (item != null)
                    {
                        item.UpdateBuyPrices(PriceType.CSDealsMarketplace, new PriceStock
                        {
                            Price = item.Currency.CalculateExchange(csDealsItem.LowestPrice.SteamPriceAsInt(), usdCurrency),
                            Stock = null
                        });
                    }
                }

                var missingItems = items.Where(x => !csDealsItems.Any(y => x.Name == y.MarketName) && x.Item.BuyPrices.ContainsKey(PriceType.CSDealsMarketplace));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(PriceType.CSDealsMarketplace, null);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item price information from CS.Deals (appId: {app.SteamId}). {ex.Message}");
                continue;
            }

            _db.SaveChanges();
        }
    }
}
