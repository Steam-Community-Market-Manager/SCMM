using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.SkinMarketgg.Client;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemPricesFromSkinMarketGGJob
{
    private readonly SteamDbContext _db;
    private readonly SkinMarketGGWebClient _skinMarketggWebClient;

    public UpdateMarketItemPricesFromSkinMarketGGJob(SteamDbContext db, SkinMarketGGWebClient skinMarketggWebClient)
    {
        _db = db;
        _skinMarketggWebClient = skinMarketggWebClient;
    }

    [Function("Update-Market-Item-Prices-From-SkinMarketGG")]
    public async Task Run([TimerTrigger("0 10-59/20 * * * *")] /* every 20mins */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Prices-From-SkinMarketGG");

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
            logger.LogTrace($"Updating item price information from skinmarket.gg (appId: {app.SteamId})");
         
            try
            {
                var skinMarketGGItems = (await _skinMarketggWebClient.GetTradeSiteInventoryAsync()) ?? new List<SkinMarketGGItem>();

                var items = await _db.SteamMarketItems
                    .Where(x => x.AppId == app.Id)
                    .Select(x => new
                    {
                        Name = x.Description.NameHash,
                        Currency = x.Currency,
                        Item = x,
                    })
                    .ToListAsync();

                foreach (var skinMarketGGItem in skinMarketGGItems)
                {
                    var item = items.FirstOrDefault(x => x.Name == skinMarketGGItem.Name)?.Item;
                    if (item != null)
                    {
                        item.UpdateBuyPrices(MarketType.skinmarketGG, new PriceWithSupply
                        {
                            Price = skinMarketGGItem.Amount > 0 ? item.Currency.CalculateExchange(skinMarketGGItem.PriceCents, usdCurrency) : 0,
                            Supply = skinMarketGGItem.Amount
                        });
                    }
                }

                var missingItems = items.Where(x => !skinMarketGGItems.Any(y => x.Name == y.Name) && x.Item.BuyPrices.ContainsKey(MarketType.skinmarketGG));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.skinmarketGG, null);
                }

                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item price information from skinmarket.gg (appId: {app.SteamId}). {ex.Message}");
                continue;
            }
        }
    }
}
