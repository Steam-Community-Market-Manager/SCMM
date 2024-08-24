using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.ShadowPay.Client;
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

public class UpdateMarketItemPricesFromShadowPay
{
    private const MarketType ShadowPay = MarketType.ShadowPay;

    private readonly SteamDbContext _db;
    private readonly ShadowPayWebClient _shadowPayWebClient;
    private readonly IStatisticsService _statisticsService;

    public UpdateMarketItemPricesFromShadowPay(SteamDbContext db, ShadowPayWebClient shadowPayWebClient, IStatisticsService statisticsService)
    {
        _db = db;
        _shadowPayWebClient = shadowPayWebClient;
        _statisticsService = statisticsService;
    }

    [Function("Update-Market-Item-Prices-From-Shadow-Pay")]
    public async Task Run([TimerTrigger("0 11/30 * * * *")] /* every 30mins */ TimerInfo timerInfo, FunctionContext context)
    {
        if (!ShadowPay.IsEnabled())
        {
            return;
        }

        var logger = context.GetLogger("Update-Market-Item-Prices-From-Shadow-Pay");

        var appIds = ShadowPay.GetSupportedAppIds().Select(x => x.ToString()).ToArray();
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
            logger.LogTrace($"Updating item price information from ShadowPay (appId: {app.SteamId})");
            await UpdateShadowPayMarketPricesForApp(logger, app, usdCurrency);
        }
    }

    private async Task UpdateShadowPayMarketPricesForApp(ILogger logger, SteamApp app, SteamCurrency usdCurrency)
    {
        var statisticsKey = String.Format(StatisticKeys.MarketStatusByAppId, app.SteamId);
        var stopwatch = new Stopwatch();
        try
        {
            stopwatch.Start();

            var shadowPayItems = (await _shadowPayWebClient.GetItemPricesAsync(app.Name)) ?? new List<ShadowPayItem>();
            var dbItems = await _db.SteamMarketItems
                .Where(x => x.AppId == app.Id)
                .Select(x => new
                {
                    Name = x.Description.NameHash,
                    Currency = x.Currency,
                    Item = x,
                })
                .ToListAsync();

            foreach (var shadowPayItem in shadowPayItems)
            {
                var item = dbItems.FirstOrDefault(x => x.Name == shadowPayItem.MarketHashName)?.Item;
                if (item != null)
                {
                    var available = shadowPayItem.Volume;
                    var price = (!String.IsNullOrEmpty(shadowPayItem.Price) ? decimal.Parse(shadowPayItem.Price) : 0) * 100;
                    item.UpdateBuyPrices(ShadowPay, new PriceWithSupply
                    {
                        Price = available > 0 && price > 0 ? item.Currency.CalculateExchange(price / usdCurrency.ExchangeRateMultiplier) : 0,
                        Supply = available
                    });
                }
            }

            var missingItems = dbItems.Where(x => !shadowPayItems.Any(y => x.Name == y.MarketHashName) && x.Item.BuyPrices.ContainsKey(ShadowPay));
            foreach (var missingItem in missingItems)
            {
                missingItem.Item.UpdateBuyPrices(ShadowPay, null);
            }

            await _db.SaveChangesAsync();

            await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, ShadowPay, x =>
            {
                x.TotalItems = shadowPayItems.Count();
                x.TotalListings = shadowPayItems.Sum(x => x.Volume);
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
                logger.LogError(ex, $"Failed to update market item price information from ShadowPay (appId: {app.SteamId}). {ex.Message}");
                await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, ShadowPay, x =>
                {
                    x.LastUpdateErrorOn = DateTimeOffset.Now;
                    x.LastUpdateError = ex.Message;
                });
            }
            catch (Exception)
            {
                logger.LogError(ex, $"Failed to update market item price statistics for ShadowPay (appId: {app.SteamId}). {ex.Message}");
            }
        }
    }
}
