using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.RustTM.Client;
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

public class UpdateMarketItemPricesFromRustTM
{
    private const MarketType RustTM = MarketType.RustTM;

    private readonly SteamDbContext _db;
    private readonly RustTMWebClient _rustTMWebClient;
    private readonly IStatisticsService _statisticsService;

    public UpdateMarketItemPricesFromRustTM(SteamDbContext db, RustTMWebClient rustTMWebClient, IStatisticsService statisticsService)
    {
        _db = db;
        _rustTMWebClient = rustTMWebClient;
        _statisticsService = statisticsService;
    }

    [Function("Update-Market-Item-Prices-From-RustTM")]
    public async Task Run([TimerTrigger("0 12/30 * * * *")] /* every 30mins */ TimerInfo timerInfo, FunctionContext context)
    {
        if (!RustTM.IsEnabled())
        {
            return;
        }

        var logger = context.GetLogger("Update-Market-Item-Prices-From-RustTM");
        var stopwatch = new Stopwatch();

        var appIds = RustTM.GetSupportedAppIds().Select(x => x.ToString()).ToArray();
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
            logger.LogTrace($"Updating item price information from Rust.tm (appId: {app.SteamId})");
            var statisticsKey = String.Format(StatisticKeys.MarketStatusByAppId, app.SteamId);

            try
            {
                stopwatch.Restart();
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
                        item.UpdateBuyPrices(RustTM, new PriceWithSupply
                        {
                            Price = rustTMItem.Volume > 0 ? item.Currency.CalculateExchange(rustTMItem.Price.ToString().SteamPriceAsInt(), usdCurrency) : 0,
                            Supply = rustTMItem.Volume
                        });
                    }
                }

                var missingItems = items.Where(x => !rustTMItems.Any(y => x.Name == y.MarketHashName) && x.Item.BuyPrices.ContainsKey(RustTM));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(RustTM, null);
                }

                await _db.SaveChangesAsync();

                await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, RustTM, x =>
                {
                    x.TotalItems = rustTMItems.Count();
                    x.TotalListings = rustTMItems.Sum(i => i.Volume);
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
                    logger.LogError(ex, $"Failed to update market item price information from Rust.tm (appId: {app.SteamId}). {ex.Message}");
                    await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, RustTM, x =>
                    {
                        x.LastUpdateErrorOn = DateTimeOffset.Now;
                        x.LastUpdateError = ex.Message;
                    });
                }
                catch (Exception)
                {
                    logger.LogError(ex, $"Failed to update market item price statistics for Rust.tm (appId: {app.SteamId}). {ex.Message}");
                }
            }
            finally
            {
                stopwatch.Stop();
            }
        }
    }
}
