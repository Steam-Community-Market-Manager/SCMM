using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.TradeSkinsFast.Client;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemPricesFromTradeSkinsFastJob
{
    private readonly SteamDbContext _db;
    private readonly TradeSkinsFastWebClient _tradeSkinsFastWebClient;

    public UpdateMarketItemPricesFromTradeSkinsFastJob(SteamDbContext db, TradeSkinsFastWebClient tradeSkinsFastWebClient)
    {
        _db = db;
        _tradeSkinsFastWebClient = tradeSkinsFastWebClient;
    }

    [Function("Update-Market-Item-Prices-From-TradeSkinsFast")]
    public async Task Run([TimerTrigger("0 15-59/20 * * * *")] /* every 20mins */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Prices-From-TradeSkinsFast");

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
            logger.LogTrace($"Updating market item price information from TradeSkinsFast (appId: {app.SteamId})");
            
            try
            {
                var tradeSkinsFastInventoryItems = (await _tradeSkinsFastWebClient.PostBotsInventoryAsync(app.SteamId))?.Items?.FirstOrDefault(x => x.Key == app.SteamId).Value ?? new TradeSkinsFastItemListing[0];
                
                var items = await _db.SteamMarketItems
                    .Where(x => x.AppId == app.Id)
                    .Select(x => new
                    {
                        Name = x.Description.NameHash,
                        Currency = x.Currency,
                        Item = x,
                    })
                    .ToListAsync();

                foreach (var tradeSkinsFastInventoryItemGroup in tradeSkinsFastInventoryItems.GroupBy(x => x.MarketName))
                {
                    var item = items.FirstOrDefault(x => x.Name == tradeSkinsFastInventoryItemGroup.Key)?.Item;
                    if (item != null)
                    {
                        var supply = tradeSkinsFastInventoryItemGroup.Sum(x => x.ItemIds?.Length ?? 0);
                        item.UpdateBuyPrices(MarketType.TradeSkinsFast, new PriceWithSupply
                        {
                            Price = supply > 0 ? item.Currency.CalculateExchange((tradeSkinsFastInventoryItemGroup.Min(x => x.ListingPrice)).ToString().SteamPriceAsInt(), usdCurrency) : 0,
                            Supply = supply
                        });
                    }
                }

                var missingItems = items.Where(x => !tradeSkinsFastInventoryItems.Any(y => x.Name == y.MarketName) && x.Item.BuyPrices.ContainsKey(MarketType.TradeSkinsFast));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.TradeSkinsFast, null);
                }

                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item price information from TradeSkinsFast (appId: {app.SteamId}). {ex.Message}");
                continue;
            }
        }
    }
}
