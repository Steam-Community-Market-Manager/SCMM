using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.CSTrade.Client;
using SCMM.Shared.Abstractions.Statistics;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Models.Statistics;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemPricesFromCSTrade
{
    private readonly SteamDbContext _db;
    private readonly CSTradeWebClient _csTradeWebClient;
    private readonly IStatisticsService _statisticsService;

    public UpdateMarketItemPricesFromCSTrade(SteamDbContext db, CSTradeWebClient csTradeWebClient, IStatisticsService statisticsService)
    {
        _db = db;
        _csTradeWebClient = csTradeWebClient;
        _statisticsService = statisticsService;
    }

    [Function("Update-Market-Item-Prices-From-CSTrade")]
    public async Task Run([TimerTrigger("0 3-59/20 * * * *")] /* every 20mins */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Prices-From-CSTrade");

        var appIds = MarketType.CSTRADE.GetSupportedAppIds().Select(x => x.ToString()).ToArray();
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
            logger.LogTrace($"Updating item price information from CS.TRADE (appId: {app.SteamId})");
            var statisticsKey = String.Format(StatisticKeys.MarketStatusByAppId, app.SteamId);

            try
            {
                var csTradeAppItems = (await _csTradeWebClient.GetPricesAsync(app.Name)) ?? new List<CSTradeItemPrice>();
                
                var items = await _db.SteamMarketItems
                    .Where(x => x.AppId == app.Id)
                    .Select(x => new
                    {
                        Name = x.Description.NameHash,
                        Currency = x.Currency,
                        Item = x,
                    })
                    .ToListAsync();

                foreach (var csTradeItem in csTradeAppItems)
                {
                    var item = items.FirstOrDefault(x => x.Name == csTradeItem.Name)?.Item;
                    if (item != null)
                    {
                        item.UpdateBuyPrices(MarketType.CSTRADE, new PriceWithSupply
                        {
                            Price = csTradeItem.Have > 0 ? item.Currency.CalculateExchange(csTradeItem.Price.ToString().SteamPriceAsInt(), usdCurrency) : 0,
                            Supply = csTradeItem.Have
                        });
                    }
                }

                var missingItems = items.Where(x => !csTradeAppItems.Any(y => x.Name == y.Name) && x.Item.BuyPrices.ContainsKey(MarketType.CSTRADE));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.CSTRADE, null);
                }

                await _db.SaveChangesAsync();

                await _statisticsService.UpdateDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, MarketType.CSTRADE, x =>
                {
                    x.TotalItems = csTradeAppItems.Count();
                    x.TotalListings = csTradeAppItems.Sum(i => i.Have);
                    x.LastUpdatedItemsOn = DateTimeOffset.Now;
                    x.LastUpdateErrorOn = null;
                    x.LastUpdateError = null;
                });
            }
            catch (Exception ex)
            {
                try
                {
                    logger.LogError(ex, $"Failed to update market item price information from CS.TRADE (appId: {app.SteamId}). {ex.Message}");
                    await _statisticsService.UpdateDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, MarketType.CSTRADE, x =>
                    {
                        x.LastUpdateErrorOn = DateTimeOffset.Now;
                        x.LastUpdateError = ex.Message;
                    });
                }
                catch (Exception)
                {
                    logger.LogError(ex, $"Failed to update market item price statistics for CS.TRADE (appId: {app.SteamId}). {ex.Message}");
                }
            }
        }
    }
}
