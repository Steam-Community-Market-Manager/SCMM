using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.Waxpeer.Client;
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

public class UpdateMarketItemPricesFromWaxpeer
{
    private const MarketType Waxpeer = MarketType.Waxpeer;

    private readonly SteamDbContext _db;
    private readonly WaxpeerWebClient _waxpeerWebClient;
    private readonly IStatisticsService _statisticsService;

    public UpdateMarketItemPricesFromWaxpeer(SteamDbContext db, WaxpeerWebClient waxpeerWebClient, IStatisticsService statisticsService)
    {
        _db = db;
        _waxpeerWebClient = waxpeerWebClient;
        _statisticsService = statisticsService;
    }

    [Function("Update-Market-Item-Prices-From-Waxpeer")]
    public async Task Run([TimerTrigger("0 24/30 * * * *")] /* every 30mins */ TimerInfo timerInfo, FunctionContext context)
    {
        if (!Waxpeer.IsEnabled())
        {
            return;
        }

        var logger = context.GetLogger("Update-Market-Item-Prices-From-Waxpeer");

        var appIds = Waxpeer.GetSupportedAppIds().Select(x => x.ToString()).ToArray();
        var supportedSteamApps = await _db.SteamApps
            .Where(x => appIds.Contains(x.SteamId))
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
            logger.LogTrace($"Updating item price information from Waxpeer (appId: {app.SteamId})");
            await UpdateWaxpeerMarketPricesForApp(logger, app, usdCurrency);
        }
    }

    private async Task UpdateWaxpeerMarketPricesForApp(ILogger logger, SteamApp app, SteamCurrency usdCurrency)
    {
        var statisticsKey = String.Format(StatisticKeys.MarketStatusByAppId, app.SteamId);
        var stopwatch = new Stopwatch();
        try
        {
            stopwatch.Start();

            var waxpeerAppItems = (await _waxpeerWebClient.GetPricesAsync(app.Name))?.Items ?? new List<WaxpeerItem>();
            var dbItems = await _db.SteamMarketItems
                .Where(x => x.AppId == app.Id)
                .Select(x => new
                {
                    Name = x.Description.NameHash,
                    Currency = x.Currency,
                    Item = x,
                })
                .ToListAsync();

            foreach (var waxpeerItem in waxpeerAppItems)
            {
                var item = dbItems.FirstOrDefault(x => x.Name == waxpeerItem.Name)?.Item;
                if (item != null)
                {
                    var price = (waxpeerItem.Min > 0 ? (long)Math.Round(waxpeerItem.Min / 10.0) : 0);
                    item.UpdateBuyPrices(Waxpeer, new PriceWithSupply
                    {
                        Price = waxpeerItem.Count > 0 ? item.Currency.CalculateExchange(price, usdCurrency) : 0,
                        Supply = waxpeerItem.Count
                    });
                }
            }

            var missingItems = dbItems.Where(x => !waxpeerAppItems.Any(y => x.Name == y.Name) && x.Item.BuyPrices.ContainsKey(Waxpeer));
            foreach (var missingItem in missingItems)
            {
                missingItem.Item.UpdateBuyPrices(Waxpeer, null);
            }

            await _db.SaveChangesAsync();

            await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, Waxpeer, x =>
            {
                x.TotalItems = waxpeerAppItems.Count();
                x.TotalListings = waxpeerAppItems.Sum(i => i.Count);
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
                logger.LogError(ex, $"Failed to update market item price information from Waxpeer (appId: {app.SteamId}). {ex.Message}");
                await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, Waxpeer, x =>
                {
                    x.LastUpdateErrorOn = DateTimeOffset.Now;
                    x.LastUpdateError = ex.Message;
                });
            }
            catch (Exception)
            {
                logger.LogError(ex, $"Failed to update market item price statistics for Waxpeer (appId: {app.SteamId}). {ex.Message}");
            }
        }
    }
}
