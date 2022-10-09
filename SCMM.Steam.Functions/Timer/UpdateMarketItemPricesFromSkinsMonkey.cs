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

public class UpdateMarketItemPricesFromSkinsMonkey
{
    private readonly SteamDbContext _db;
    private readonly SkinsMonkeyWebClient _skinsMonkeyWebClient;

    public UpdateMarketItemPricesFromSkinsMonkey(SteamDbContext db, SkinsMonkeyWebClient skinsMonkeyWebClient)
    {
        _db = db;
        _skinsMonkeyWebClient = skinsMonkeyWebClient;
    }

    [Function("Update-Market-Item-Prices-From-SkinsMonkey")]
    public async Task Run([TimerTrigger("0 12-59/20 * * * *")] /* every 20mins */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Prices-From-SkinsMonkey");

        var supportedSteamApps = await _db.SteamApps
            .Where(x => x.SteamId == Constants.CSGOAppId.ToString() || x.SteamId == Constants.RustAppId.ToString())
            .Where(x => x.IsActive)
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
            logger.LogTrace($"Updating item price information from SkinsMonkey (appId: {app.SteamId})");

            try
            {
                var skinsMonkeyItems = await _skinsMonkeyWebClient.GetItemPricesAsync(app.SteamId);

                var items = await _db.SteamMarketItems
                    .Where(x => x.AppId == app.Id)
                    .Select(x => new
                    {
                        Name = x.Description.NameHash,
                        Currency = x.Currency,
                        Item = x,
                    })
                    .ToListAsync();

                foreach (var skinsMonkeyItem in skinsMonkeyItems)
                {
                    var item = items.FirstOrDefault(x => x.Name == skinsMonkeyItem.MarketHashName)?.Item;
                    if (item != null)
                    {
                        item.UpdateBuyPrices(MarketType.SkinsMonkey, new PriceWithSupply
                        {
                            Price = skinsMonkeyItem.Stock > 0 ? item.Currency.CalculateExchange(skinsMonkeyItem.PriceTrade, usdCurrency) : 0,
                            Supply = skinsMonkeyItem.Stock
                        });
                    }
                }

                var missingItems = items.Where(x => !skinsMonkeyItems.Any(y => x.Name == y.MarketHashName) && x.Item.BuyPrices.ContainsKey(MarketType.SkinsMonkey));
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
