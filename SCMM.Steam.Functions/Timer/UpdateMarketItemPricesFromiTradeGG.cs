using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.iTradegg.Client;
using SCMM.Shared.Abstractions.Statistics;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Models.Statistics;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;
using System.Diagnostics;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemPricesFromiTradeggJob
{
    private const MarketType iTradegg = MarketType.iTradegg;

    private readonly SteamDbContext _db;
    private readonly iTradeggWebClient _iTradeggWebClient;
    private readonly IStatisticsService _statisticsService;

    public UpdateMarketItemPricesFromiTradeggJob(SteamDbContext db, iTradeggWebClient iTradeggWebClient, IStatisticsService statisticsService)
    {
        _db = db;
        _iTradeggWebClient = iTradeggWebClient;
        _statisticsService = statisticsService;
    }

    [Function("Update-Market-Item-Prices-From-iTradegg")]
    public async Task Run([TimerTrigger("0 16/30 * * * *")] /* every 30mins */ TimerInfo timerInfo, FunctionContext context)
    {
        if (!iTradegg.IsEnabled())
        {
            return;
        }

        var logger = context.GetLogger("Update-Market-Item-Prices-From-iTradegg");

        var appIds = iTradegg.GetSupportedAppIds().Select(x => x.ToString()).ToArray();
        var supportedSteamApps = await _db.SteamApps
            .Where(x => appIds.Contains(x.SteamId))
            //.Where(x => x.IsActive)
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
            logger.LogTrace($"Updating item price information from iTrade.gg (appId: {app.SteamId})");
            await UpdateiTradeGGMarketPricesForApp(logger, app, usdCurrency);
        }
    }

    private async Task UpdateiTradeGGMarketPricesForApp(ILogger logger, SteamApp app, SteamCurrency usdCurrency)
    {
        var statisticsKey = String.Format(StatisticKeys.MarketStatusByAppId, app.SteamId);
        var stopwatch = new Stopwatch();
        try
        {
            stopwatch.Start();

            var iTradeggItems = (await _iTradeggWebClient.GetInventoryAsync(app.SteamId)) ?? new List<iTradeggItem>();
            var dbItems = await _db.SteamMarketItems
                .Where(x => x.AppId == app.Id)
                .Select(x => new
                {
                    Name = x.Description.NameHash,
                    Currency = x.Currency,
                    Item = x,
                })
                .ToListAsync();

            foreach (var iTradeggItem in iTradeggItems)
            {
                var item = dbItems.FirstOrDefault(x => x.Name == iTradeggItem.Name)?.Item;
                if (item != null)
                {
                    item.UpdateBuyPrices(iTradegg, new PriceWithSupply
                    {
                        Price = iTradeggItem.Same > 0 ? item.Currency.CalculateExchange(iTradeggItem.Price.ToString().SteamPriceAsInt(), usdCurrency) : 0,
                        Supply = iTradeggItem.Same
                    });
                }
            }

            var missingItems = dbItems.Where(x => !iTradeggItems.Any(y => x.Name == y.Name) && x.Item.BuyPrices.ContainsKey(iTradegg));
            foreach (var missingItem in missingItems)
            {
                missingItem.Item.UpdateBuyPrices(iTradegg, null);
            }

            await _db.SaveChangesAsync();

            await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, iTradegg, x =>
            {
                x.TotalItems = iTradeggItems.Count();
                x.TotalListings = iTradeggItems.Sum(i => i.Same);
                x.LastUpdatedItemsOn = DateTimeOffset.Now;
                x.LastUpdatedItemsDuration = stopwatch.Elapsed;
                x.LastUpdateErrorOn = null;
                x.LastUpdateError = null;
            });
        }
        catch (Exception ex)
        {
            try
            {
                logger.LogError(ex, $"Failed to update market item price information from iTrade.gg (appId: {app.SteamId}). {ex.Message}");
                await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, iTradegg, x =>
                {
                    x.LastUpdateErrorOn = DateTimeOffset.Now;
                    x.LastUpdateError = ex.Message;
                });
            }
            catch (Exception)
            {
                logger.LogError(ex, $"Failed to update market item price statistics for iTrade.gg (appId: {app.SteamId}). {ex.Message}");
            }
        }
    }
}
