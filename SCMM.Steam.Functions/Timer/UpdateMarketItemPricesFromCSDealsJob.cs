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
            logger.LogTrace($"Updating market item price information from CS.Deals (appId: {app.SteamId})");
            var items = await _db.SteamMarketItems
                .Select(x => new
                {
                    Name = x.Description.NameHash,
                    Currency = x.Currency,
                    Item = x,
                })
                .ToListAsync();

            try
            {
                var csDealsInventoryItems = (await _csDealsWebClient.BotsInventoryAsync(app.SteamId))?.Items?.FirstOrDefault(x => x.Key == app.SteamId).Value;
                if (csDealsInventoryItems?.Any() != true)
                {
                    continue;
                }

                foreach (var csDealsInventoryItemGroup in csDealsInventoryItems.GroupBy(x => x.MarketName))
                {
                    var item = items.FirstOrDefault(x => x.Name == csDealsInventoryItemGroup.Key)?.Item;
                    if (item != null)
                    {
                        var stock = csDealsInventoryItemGroup.Sum(x => x.ItemIds?.Length ?? 0);
                        item.UpdateBuyPrices(MarketType.CSDealsTrade, new PriceStock
                        {
                            Price = stock > 0 ? item.Currency.CalculateExchange((csDealsInventoryItemGroup.Min(x => x.ListingPrice)).ToString().SteamPriceAsInt(), usdCurrency) : 0,
                            Stock = stock
                        });
                    }
                }

                var missingItems = items.Where(x => !csDealsInventoryItems.Any(y => x.Name == y.MarketName) && x.Item.BuyPrices.ContainsKey(MarketType.CSDealsTrade));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.CSDealsTrade, null);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item price information from CS.Deals (appId: {app.SteamId}, source: trade inventory). {ex.Message}");
                continue;
            }

            try
            {
                var csDealsLowestPriceItems = await _csDealsWebClient.PricingGetLowestPricesAsync(app.SteamId);
                if (csDealsLowestPriceItems?.Any() != true)
                {
                    continue;
                }

                foreach (var csDealsLowestPriceItem in csDealsLowestPriceItems)
                {
                    var item = items.FirstOrDefault(x => x.Name == csDealsLowestPriceItem.MarketName)?.Item;
                    if (item != null)
                    {
                        item.UpdateBuyPrices(MarketType.CSDealsMarketplace, new PriceStock
                        {
                            Price = item.Currency.CalculateExchange(csDealsLowestPriceItem.LowestPrice.SteamPriceAsInt(), usdCurrency),
                            Stock = null
                        });
                    }
                }

                var missingItems = items.Where(x => !csDealsLowestPriceItems.Any(y => x.Name == y.MarketName) && x.Item.BuyPrices.ContainsKey(MarketType.CSDealsMarketplace));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.CSDealsMarketplace, null);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item price information from CS.Deals (appId: {app.SteamId}, source: lowest price items). {ex.Message}");
                continue;
            }

            _db.SaveChanges();
        }
    }
}
