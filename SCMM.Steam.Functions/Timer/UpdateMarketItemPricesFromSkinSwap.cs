using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.SkinSwap.Client;
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

public class UpdateMarketItemPricesFromSkinSwap
{
    private const MarketType SkinSwap = MarketType.SkinSwap;

    private readonly SteamDbContext _db;
    private readonly SkinSwapWebClient _skinSwapWebClient;
    private readonly IStatisticsService _statisticsService;

    public UpdateMarketItemPricesFromSkinSwap(SteamDbContext db, SkinSwapWebClient skinSwapWebClient, IStatisticsService statisticsService)
    {
        _db = db;
        _skinSwapWebClient = skinSwapWebClient;
        _statisticsService = statisticsService;
    }

    [Function("Update-Market-Item-Prices-From-SkinSwap")]
    public async Task Run([TimerTrigger("0 20/30 * * * *")] /* every 30mins */ TimerInfo timerInfo, FunctionContext context)
    {
        if (!SkinSwap.IsEnabled())
        {
            return;
        }

        var logger = context.GetLogger("Update-Market-Item-Prices-From-SkinSwap");

        var appIds = SkinSwap.GetSupportedAppIds().Select(x => x.ToString()).ToArray();
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

        var skinSwapItems = Enumerable.Empty<SkinSwapItem>();
        foreach (var app in supportedSteamApps)
        {
            logger.LogTrace($"Updating item price information from SkinSwap (appId: {app.SteamId})");
            skinSwapItems = await UpdateSkinSwapMarketPricesForApp(logger, app, usdCurrency, skinSwapItems);
        }
    }

    private async Task<IEnumerable<SkinSwapItem>> UpdateSkinSwapMarketPricesForApp(ILogger logger, SteamApp app, SteamCurrency usdCurrency, IEnumerable<SkinSwapItem> skinSwapItems)
    {
        var statisticsKey = String.Format(StatisticKeys.MarketStatusByAppId, app.SteamId);
        var stopwatch = new Stopwatch();
        try
        {
            stopwatch.Start();

            // The SkinSwap API returns all items for all apps, so make sure we only request them once even if this function is called multiple times (for multiple app updates)
            if (!skinSwapItems.Any())
            {
                skinSwapItems = await _skinSwapWebClient.GetItemsAsync();
            }

            var dbItems = await _db.SteamMarketItems
                .Where(x => x.AppId == app.Id)
                .Select(x => new
                {
                    Name = x.Description.NameHash,
                    Currency = x.Currency,
                    Item = x,
                })
                .ToListAsync();

            var skinSwapItemGroups = skinSwapItems.GroupBy(x => x.MarketHashName);
            foreach (var skinSwapItemGroup in skinSwapItemGroups)
            {
                var item = dbItems.FirstOrDefault(x => x.Name == skinSwapItemGroup.Key)?.Item;
                if (item != null)
                {
                    var supply = skinSwapItemGroup.Sum(x => x.Amount);
                    var price = skinSwapItemGroup.Min(x => x.Price);
                    item.UpdateBuyPrices(SkinSwap, new PriceWithSupply
                    {
                        Price = supply > 0 && price > 0 ? item.Currency.CalculateExchange(price / usdCurrency.ExchangeRateMultiplier) : 0,
                        Supply = supply
                    });
                }
            }

            var missingItems = dbItems.Where(x => !skinSwapItems.Any(y => x.Name == y.MarketHashName) && x.Item.BuyPrices.ContainsKey(SkinSwap));
            foreach (var missingItem in missingItems)
            {
                missingItem.Item.UpdateBuyPrices(SkinSwap, null);
            }

            await _db.SaveChangesAsync();

            await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, SkinSwap, x =>
            {
                x.TotalItems = skinSwapItemGroups.Count();
                x.TotalListings = skinSwapItemGroups.Sum(x => x.Sum(y => y.Amount));
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
                logger.LogError(ex, $"Failed to update market item price information from SkinSwap (appId: {app.SteamId}). {ex.Message}");
                await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, SkinSwap, x =>
                {
                    x.LastUpdateErrorOn = DateTimeOffset.Now;
                    x.LastUpdateError = ex.Message;
                });
            }
            catch (Exception)
            {
                logger.LogError(ex, $"Failed to update market item price statistics for SkinSwap (appId: {app.SteamId}). {ex.Message}");
            }
        }
        finally
        {
            stopwatch.Stop();
        }

        return skinSwapItems;
    }
}
