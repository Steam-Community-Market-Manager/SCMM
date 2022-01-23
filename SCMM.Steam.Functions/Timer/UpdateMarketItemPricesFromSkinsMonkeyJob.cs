using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.SkinsMonkey.Client;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemPricesFromSkinsMonkeyJob
{
    private readonly SteamDbContext _db;
    private readonly SkinsMonkeyWebClient _skinsMonkeyWebClient;

    public UpdateMarketItemPricesFromSkinsMonkeyJob(SteamDbContext db, SkinsMonkeyWebClient skinsMonkeyWebClient)
    {
        _db = db;
        _skinsMonkeyWebClient = skinsMonkeyWebClient;
    }

    [Function("Update-Market-Item-Prices-From-SkinsMonkey")]
    public async Task Run([TimerTrigger("0 11-59/15 * * * *")] /* every 15mins */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Prices-From-SkinsMonkey");

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
            logger.LogTrace($"Updating item price information from SkinsMonkey (appId: {app.SteamId})");
            
            try
            {
                var skinsMonkeyItems = new List<SkinsMonkeyItemListing>();
                var inventoryItems = (IEnumerable<SkinsMonkeyItemListing>) null;
                var inventoryOffset = 0;
                do
                {
                    // NOTE: Items have to be fetched in multiple batches, keep reading until no new items are found
                    inventoryItems = await _skinsMonkeyWebClient.GetInventoryAsync(app.SteamId, offset: inventoryOffset, limit: SkinsMonkeyWebClient.MaxPageLimit);
                    if (inventoryItems?.Any() == true)
                    {
                        skinsMonkeyItems.AddRange(inventoryItems);
                        inventoryOffset += SkinsMonkeyWebClient.MaxPageLimit;
                    }
                } while (inventoryItems?.Any() == true);
                
                var items = await _db.SteamMarketItems
                    .Where(x => x.AppId == app.Id)
                    .Select(x => new
                    {
                        Name = x.Description.NameHash,
                        Currency = x.Currency,
                        Item = x,
                    })
                    .ToListAsync();

                foreach (var skinsMonkeyItem in skinsMonkeyItems.GroupBy(x => x.Item.MarketName))
                {
                    var item = items.FirstOrDefault(x => x.Name == skinsMonkeyItem.Key)?.Item;
                    if (item != null)
                    {
                        var supply = skinsMonkeyItem.Count();
                        item.UpdateBuyPrices(MarketType.SkinsMonkey, new PriceWithSupply
                        {
                            Price = supply > 0 ? item.Currency.CalculateExchange(skinsMonkeyItem.Min(x => x.Item.Price), usdCurrency) : 0,
                            Supply = supply
                        });
                    }
                }

                var missingItems = items.Where(x => !skinsMonkeyItems.Any(y => x.Name == y.Item.MarketName) && x.Item.BuyPrices.ContainsKey(MarketType.SkinsMonkey));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.SkinsMonkey, null);
                }

                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item price information from SkinsMonkey (appId: {app.SteamId}). {ex.Message}");
                continue;
            }
        }
    }
}
