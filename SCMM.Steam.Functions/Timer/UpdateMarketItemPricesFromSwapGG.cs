using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.SwapGG.Client;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemPricesFromSwapGG
{
    private readonly SteamDbContext _db;
    private readonly SwapGGWebClient _swapGGWebClient;

    public UpdateMarketItemPricesFromSwapGG(SteamDbContext db, SwapGGWebClient swapGGWebClient)
    {
        _db = db;
        _swapGGWebClient = swapGGWebClient;
    }

    [Function("Update-Market-Item-Prices-From-SwapGG")]
    public async Task Run([TimerTrigger("0 14-59/20 * * * *")] /* every 20mins */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Prices-From-SwapGG");

        var supportedSteamApps = await _db.SteamApps
            .Where(x => x.SteamId == Constants.CSGOAppId.ToString() || x.SteamId == Constants.RustAppId.ToString())
            .Where(x => x.IsActive)
            .ToListAsync();
        if (!supportedSteamApps.Any())
        {
            return;
        }

        // Prices are returned in EUR by default
        var eurCurrency = _db.SteamCurrencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyEUR);
        if (eurCurrency == null)
        {
            return;
        }

        foreach (var app in supportedSteamApps)
        {
            logger.LogTrace($"Updating market item price information from swap.gg (appId: {app.SteamId})");

            try
            {
                var swapggTradeItems = (await _swapGGWebClient.GetTradeBotInventoryAsync(app.SteamId)) ?? new List<SwapGGTradeItem>();

                var items = await _db.SteamMarketItems
                  .Where(x => x.AppId == app.Id)
                  .Select(x => new
                  {
                      Name = x.Description.NameHash,
                      Currency = x.Currency,
                      Item = x,
                  })
                  .ToListAsync();

                foreach (var swapggTradeItem in swapggTradeItems)
                {
                    var item = items.FirstOrDefault(x => x.Name == swapggTradeItem.Name)?.Item;
                    if (item != null)
                    {
                        item.UpdateBuyPrices(MarketType.SwapGGTrade, new PriceWithSupply
                        {
                            Price = swapggTradeItem.ItemIds?.Length > 0 ? item.Currency.CalculateExchange(swapggTradeItem.Price, eurCurrency) : 0,
                            Supply = swapggTradeItem.ItemIds?.Length
                        });
                    }
                }

                var missingItems = items.Where(x => !swapggTradeItems.Any(y => x.Name == y.Name) && x.Item.BuyPrices.ContainsKey(MarketType.SwapGGTrade));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.SwapGGTrade, null);
                }

                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item price information from swap.gg (appId: {app.SteamId}, source: trade inventory). {ex.Message}");
            }

            try
            {
                var swapggMarketItems = (await _swapGGWebClient.GetMarketPricingLowestAsync(app.SteamId)) ?? new Dictionary<string, SwapGGMarketItem>();

                var items = await _db.SteamMarketItems
                  .Where(x => x.AppId == app.Id)
                  .Select(x => new
                  {
                      Name = x.Description.NameHash,
                      Currency = x.Currency,
                      Item = x,
                  })
                  .ToListAsync();

                foreach (var swapggMarketItem in swapggMarketItems)
                {
                    var item = items.FirstOrDefault(x => x.Name == swapggMarketItem.Key)?.Item;
                    if (item != null)
                    {
                        item.UpdateBuyPrices(MarketType.SwapGGMarket, new PriceWithSupply
                        {
                            Price = swapggMarketItem.Value.Quantity > 0 ? item.Currency.CalculateExchange(swapggMarketItem.Value.Price, eurCurrency) : 0,
                            Supply = swapggMarketItem.Value.Quantity
                        });
                    }
                }

                var missingItems = items.Where(x => !swapggMarketItems.Any(y => x.Name == y.Key) && x.Item.BuyPrices.ContainsKey(MarketType.SwapGGMarket));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.SwapGGMarket, null);
                }

                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item price information from swap.gg (appId: {app.SteamId}, source: market). {ex.Message}");
            }
        }
    }
}
