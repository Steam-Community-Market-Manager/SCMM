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

public class UpdateMarketItemPricesFromCSDeals
{
    private readonly SteamDbContext _db;
    private readonly CSDealsWebClient _csDealsWebClient;

    public UpdateMarketItemPricesFromCSDeals(SteamDbContext db, CSDealsWebClient csDealsWebClient)
    {
        _db = db;
        _csDealsWebClient = csDealsWebClient;
    }

    [Function("Update-Market-Item-Prices-From-CSDeals")]
    public async Task Run([TimerTrigger("0 2-59/20 * * * *")] /* every 20mins */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Prices-From-CSDeals");

        var supportedSteamApps = await _db.SteamApps
            .Where(x => x.SteamId == Constants.CSGOAppId.ToString() || x.SteamId == Constants.RustAppId.ToString())
            .ToListAsync();
        if (!supportedSteamApps.Any())
        {
            return;
        }

        // Prices are returned in USD by default
        var usdCurrency = _db.SteamCurrencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyUSD);
        if (usdCurrency == null)
        {
            return;
        }

        foreach (var app in supportedSteamApps)
        {
            logger.LogTrace($"Updating market item price information from CS.Deals (appId: {app.SteamId})");
            
            try
            {
                var csDealsInventoryItems = (await _csDealsWebClient.PostBotsInventoryAsync(app.SteamId))?.Items?.FirstOrDefault(x => x.Key == app.SteamId).Value ?? new CSDealsItemListing[0];

                var items = await _db.SteamMarketItems
                    .Where(x => x.AppId == app.Id)
                    .Select(x => new
                    {
                        Name = x.Description.NameHash,
                        Currency = x.Currency,
                        Item = x,
                    })
                    .ToListAsync();

                foreach (var csDealsInventoryItemGroup in csDealsInventoryItems.GroupBy(x => x.MarketName))
                {
                    var item = items.FirstOrDefault(x => x.Name == csDealsInventoryItemGroup.Key)?.Item;
                    if (item != null)
                    {
                        var supply = csDealsInventoryItemGroup.Sum(x => x.ItemIds?.Length ?? 0);
                        item.UpdateBuyPrices(MarketType.CSDealsTrade, new PriceWithSupply
                        {
                            Price = supply > 0 ? item.Currency.CalculateExchange((csDealsInventoryItemGroup.Min(x => x.ListingPrice)).ToString().SteamPriceAsInt(), usdCurrency) : 0,
                            Supply = supply
                        });
                    }
                }

                var missingItems = items.Where(x => !csDealsInventoryItems.Any(y => x.Name == y.MarketName) && x.Item.BuyPrices.ContainsKey(MarketType.CSDealsTrade));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.CSDealsTrade, null);
                }

                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item price information from CS.Deals (appId: {app.SteamId}, source: trade inventory). {ex.Message}");
                continue;
            }

            try
            {
                var csDealsLowestPriceItems = (await _csDealsWebClient.GetPricingGetLowestPricesAsync(app.SteamId)) ?? new List<CSDealsItemPrice>();
                
                var items = await _db.SteamMarketItems
                    .Where(x => x.AppId == app.Id)
                    .Select(x => new
                    {
                        Name = x.Description.NameHash,
                        Currency = x.Currency,
                        Item = x,
                    })
                    .ToListAsync();

                foreach (var csDealsLowestPriceItem in csDealsLowestPriceItems)
                {
                    var item = items.FirstOrDefault(x => x.Name == csDealsLowestPriceItem.MarketName)?.Item;
                    if (item != null)
                    {
                        item.UpdateBuyPrices(MarketType.CSDealsMarketplace, new PriceWithSupply
                        {
                            Price = item.Currency.CalculateExchange(csDealsLowestPriceItem.LowestPrice.SteamPriceAsInt(), usdCurrency),
                            Supply = null
                        });
                    }
                }

                var missingItems = items.Where(x => !csDealsLowestPriceItems.Any(y => x.Name == y.MarketName) && x.Item.BuyPrices.ContainsKey(MarketType.CSDealsMarketplace));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.CSDealsMarketplace, null);
                }

                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item price information from CS.Deals (appId: {app.SteamId}, source: lowest price items). {ex.Message}");
                continue;
            }
        }
    }
}
