using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.SkinsMonkey.Client;
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

public class UpdateMarketItemPricesFromSkinsMonkey
{
    private const MarketType SkinsMonkey = MarketType.SkinsMonkey;

    private readonly SteamDbContext _db;
    private readonly SkinsMonkeyWebClient _skinsMonkeyWebClient;
    private readonly IStatisticsService _statisticsService;

    public UpdateMarketItemPricesFromSkinsMonkey(SteamDbContext db, SkinsMonkeyWebClient skinsMonkeyWebClient, IStatisticsService statisticsService)
    {
        _db = db;
        _skinsMonkeyWebClient = skinsMonkeyWebClient;
        _statisticsService = statisticsService;
    }

    [Function("Update-Market-Item-Prices-From-SkinsMonkey")]
    public async Task Run([TimerTrigger("0 18/30 * * * *")] /* every 30mins */ TimerInfo timerInfo, FunctionContext context)
    {
        if (!SkinsMonkey.IsEnabled())
        {
            return;
        }

        var logger = context.GetLogger("Update-Market-Item-Prices-From-SkinsMonkey");

        var appIds = SkinsMonkey.GetSupportedAppIds().Select(x => x.ToString()).ToArray();
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
            logger.LogTrace($"Updating item price information from SkinsMonkey (appId: {app.SteamId})");
            await UpdateSkinsMonkeyMarketPricesForApp(logger, app, usdCurrency);
        }
    }

    private async Task UpdateSkinsMonkeyMarketPricesForApp(ILogger logger, SteamApp app, SteamCurrency usdCurrency)
    {
        var statisticsKey = String.Format(StatisticKeys.MarketStatusByAppId, app.SteamId);
        var stopwatch = new Stopwatch();
        try
        {
            stopwatch.Start();

            var skinsMonkeyItems = await _skinsMonkeyWebClient.GetItemPricesAsync(app.SteamId);
            var dbItems = await _db.SteamMarketItems
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
                var item = dbItems.FirstOrDefault(x => x.Name == skinsMonkeyItem.MarketHashName)?.Item;
                if (item != null)
                {
                    item.UpdateBuyPrices(SkinsMonkey, new PriceWithSupply
                    {
                        Price = skinsMonkeyItem.Stock > 0 ? item.Currency.CalculateExchange(skinsMonkeyItem.PriceTrade, usdCurrency) : 0,
                        Supply = skinsMonkeyItem.Stock
                    });
                }
            }

            var missingItems = dbItems.Where(x => !skinsMonkeyItems.Any(y => x.Name == y.MarketHashName) && x.Item.BuyPrices.ContainsKey(SkinsMonkey));
            foreach (var missingItem in missingItems)
            {
                missingItem.Item.UpdateBuyPrices(SkinsMonkey, null);
            }

            await _db.SaveChangesAsync();

            await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, SkinsMonkey, x =>
            {
                x.TotalItems = skinsMonkeyItems.Count();
                x.TotalListings = skinsMonkeyItems.Sum(i => i.Stock);
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
                logger.LogError(ex, $"Failed to update market item price information from SkinsMonkey (appId: {app.SteamId}). {ex.Message}");
                await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, SkinsMonkey, x =>
                {
                    x.LastUpdateErrorOn = DateTimeOffset.Now;
                    x.LastUpdateError = ex.Message;
                });
            }
            catch (Exception)
            {
                logger.LogError(ex, $"Failed to update market item price statistics for SkinsMonkey (appId: {app.SteamId}). {ex.Message}");
            }
        }
    }
}
