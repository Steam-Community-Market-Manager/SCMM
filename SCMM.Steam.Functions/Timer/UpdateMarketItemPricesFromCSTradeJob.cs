using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.CSTrade.Client;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemPricesFromCSTradeJob
{
    private readonly SteamDbContext _db;
    private readonly CSTradeWebClient _csTradeWebClient;

    public UpdateMarketItemPricesFromCSTradeJob(SteamDbContext db, CSTradeWebClient csTradeWebClient)
    {
        _db = db;
        _csTradeWebClient = csTradeWebClient;
    }

    [Function("Update-Market-Item-Prices-From-CSTrade")]
    public async Task Run([TimerTrigger("0 3-59/15 * * * *")] /* every 15mins */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Prices-From-CSTrade");

        var steamApps = await _db.SteamApps
            .Where(x => x.IsActive)
            .ToListAsync();
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
            logger.LogTrace($"Updating item price information from CS.TRADE (appId: {app.SteamId})");
            var items = await _db.SteamMarketItems
                .Where(x => x.AppId == app.Id)
                .Select(x => new
                {
                    Name = x.Description.NameHash,
                    Currency = x.Currency,
                    Item = x,
                })
                .ToListAsync();

            try
            {
                var csTradeItems = await _csTradeWebClient.GetInventoryAsync();
                var csTradeAppItems = csTradeItems.Where(x => x.AppId == app.SteamId).Where(x => x.Price != null).ToList();
                foreach (var csTradeItem in csTradeAppItems.GroupBy(x => x.MarketHashName))
                {
                    var item = items.FirstOrDefault(x => x.Name == csTradeItem.Key)?.Item;
                    if (item != null)
                    {
                        var supply = csTradeItem.Count();
                        item.UpdateBuyPrices(MarketType.CSTRADE, new PriceWithSupply
                        {
                            Price = supply > 0 ? item.Currency.CalculateExchange(csTradeItem.Min(x => x.Price).ToString().SteamPriceAsInt(), usdCurrency) : 0,
                            Supply = supply
                        });
                    }
                }

                var missingItems = items.Where(x => !csTradeItems.Any(y => x.Name == y.MarketHashName) && x.Item.BuyPrices.ContainsKey(MarketType.CSTRADE));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.CSTRADE, null);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item price information from CS.TRADE (appId: {app.SteamId}). {ex.Message}");
                continue;
            }

            _db.SaveChanges();
        }
    }
}
