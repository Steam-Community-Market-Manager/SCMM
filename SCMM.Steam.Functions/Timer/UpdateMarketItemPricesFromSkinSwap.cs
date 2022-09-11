using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.SkinSwap.Client;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemPricesFromSkinSwap
{
    private readonly SteamDbContext _db;
    private readonly SkinSwapWebClient _skinSwapWebClient;

    public UpdateMarketItemPricesFromSkinSwap(SteamDbContext db, SkinSwapWebClient skinSwapWebClient)
    {
        _db = db;
        _skinSwapWebClient = skinSwapWebClient;
    }

    [Function("Update-Market-Item-Prices-From-SkinSwap")]
    public async Task Run([TimerTrigger("0 13-59/20 * * * *")] /* every 20mins */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Prices-From-SkinSwap");

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

        var skinSwapItems = (await _skinSwapWebClient.GetSiteInventoryAsync()) ?? new Dictionary<string, SkinSwapItem[]>();
        foreach (var app in supportedSteamApps)
        {
            logger.LogTrace($"Updating item price information from SkinSwap (appId: {app.SteamId})");
         
            try
            {
                var skinSwapAppItems = skinSwapItems.Where(x => x.Key == app.SteamId).SelectMany(x => x.Value).ToList();

                var items = await _db.SteamMarketItems
                    .Where(x => x.AppId == app.Id)
                    .Select(x => new
                    {
                        Name = x.Description.NameHash,
                        Currency = x.Currency,
                        Item = x,
                    })
                    .ToListAsync();

                foreach (var skinSwapItemGroup in skinSwapAppItems.GroupBy(x => x.MarketName))
                {
                    var item = items.FirstOrDefault(x => x.Name == skinSwapItemGroup.Key)?.Item;
                    if (item != null)
                    {
                        var supply = skinSwapItemGroup.Count();
                        item.UpdateBuyPrices(MarketType.SkinSwap, new PriceWithSupply
                        {
                            Price = supply > 0 ? item.Currency.CalculateExchange(skinSwapItemGroup.Min(x => x.PriceListed).ToString().SteamPriceAsInt(), usdCurrency) : 0,
                            Supply = supply
                        });
                    }
                }

                var missingItems = items.Where(x => !skinSwapAppItems.Any(y => x.Name == y.MarketName) && x.Item.BuyPrices.ContainsKey(MarketType.SkinSwap));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.SkinSwap, null);
                }

                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item price information from SkinSwap (appId: {app.SteamId}). {ex.Message}");
                continue;
            }
        }
    }
}
