using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.RustTM.Client;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemPricesFromRustTM
{
    private readonly SteamDbContext _db;
    private readonly RustTMWebClient _rustTMWebClient;

    public UpdateMarketItemPricesFromRustTM(SteamDbContext db, RustTMWebClient rustTMWebClient)
    {
        _db = db;
        _rustTMWebClient = rustTMWebClient;
    }

    [Function("Update-Market-Item-Prices-From-RustTM")]
    public async Task Run([TimerTrigger("0 8-59/20 * * * *")] /* every 20mins */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Prices-From-RustTM");

        var appIds = MarketType.RustTM.GetMarketAppIds().Select(x => x.ToString()).ToArray();
        var supportedSteamApps = await _db.SteamApps
            .Where(x => appIds.Contains(x.SteamId))
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
            logger.LogTrace($"Updating item price information from Rust.tm (appId: {app.SteamId})");

            try
            {
                var rustTMItems = (await _rustTMWebClient.GetPricesAsync(usdCurrency.Name)) ?? new List<RustTMItem>();

                var items = await _db.SteamMarketItems
                   .Where(x => x.AppId == app.Id)
                   .Select(x => new
                   {
                       Name = x.Description.NameHash,
                       Currency = x.Currency,
                       Item = x,
                   })
                   .ToListAsync();

                foreach (var rustTMItem in rustTMItems)
                {
                    var item = items.FirstOrDefault(x => x.Name == rustTMItem.MarketHashName)?.Item;
                    if (item != null)
                    {
                        item.UpdateBuyPrices(MarketType.RustTM, new PriceWithSupply
                        {
                            Price = rustTMItem.Volume > 0 ? item.Currency.CalculateExchange(rustTMItem.Price.ToString().SteamPriceAsInt(), usdCurrency) : 0,
                            Supply = rustTMItem.Volume
                        });
                    }
                }

                var missingItems = items.Where(x => !rustTMItems.Any(y => x.Name == y.MarketHashName) && x.Item.BuyPrices.ContainsKey(MarketType.RustTM));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.RustTM, null);
                }

                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item price information from Rust.tm (appId: {app.SteamId}). {ex.Message}");
                continue;
            }
        }
    }
}
