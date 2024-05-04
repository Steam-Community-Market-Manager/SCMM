using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.ManncoStore.Client;
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

public class UpdateMarketItemPricesFromManncoStore
{
    private const MarketType ManncoStore = MarketType.ManncoStore;

    private readonly SteamDbContext _db;
    private readonly ManncoStoreWebClient _manncoStoreWebClient;
    private readonly IStatisticsService _statisticsService;

    public UpdateMarketItemPricesFromManncoStore(SteamDbContext db, ManncoStoreWebClient manncoStoreWebClient, IStatisticsService statisticsService)
    {
        _db = db;
        _manncoStoreWebClient = manncoStoreWebClient;
        _statisticsService = statisticsService;
    }

    [Function("Update-Market-Item-Prices-From-Mannco-Store")]
    public async Task Run([TimerTrigger("0 26/30 * * * *")] /* every 30mins */ TimerInfo timerInfo, FunctionContext context)
    {
        if (!ManncoStore.IsEnabled())
        {
            return;
        }

        var logger = context.GetLogger("Update-Market-Item-Prices-From-Mannco-Store");

        var appIds = ManncoStore.GetSupportedAppIds().Select(x => x.ToString()).ToArray();
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
            logger.LogTrace($"Updating item price information from Mannco.store (appId: {app.SteamId})");
            await UpdateManncoStoreMarketPricesForApp(logger, app, usdCurrency);
        }
    }

    private async Task UpdateManncoStoreMarketPricesForApp(ILogger logger, SteamApp app, SteamCurrency usdCurrency)
    {
        var statisticsKey = String.Format(StatisticKeys.MarketStatusByAppId, app.SteamId);
        var stopwatch = new Stopwatch();
        try
        {
            stopwatch.Start();

            var manncoStoreItems = new List<ManncoStoreItem>();
            var itemsResponse = new ManncoStoreItem[0];
            var skip = 0;
            do
            {
                // NOTE: Items must be fetched across multiple pages, we keep reading until no new items/pages are found
                itemsResponse = (await _manncoStoreWebClient.GetItemsAsync(app.SteamId, skip))?.ToArray();
                if (itemsResponse?.Any() == true)
                {
                    manncoStoreItems.AddRange(itemsResponse);
                    skip += itemsResponse.Length;
                }
            } while (itemsResponse != null && itemsResponse.Length > 0);

            var dbItems = await _db.SteamMarketItems
                .Where(x => x.AppId == app.Id)
                .Select(x => new
                {
                    Name = x.Description.NameHash,
                    Currency = x.Currency,
                    Item = x,
                })
                .ToListAsync();

            foreach (var manncoStoreItem in manncoStoreItems)
            {
                var item = dbItems.FirstOrDefault(x => x.Name == manncoStoreItem.Name)?.Item;
                if (item != null)
                {
                    var available = manncoStoreItem.Count;
                    var price = manncoStoreItem.Price;
                    item.UpdateBuyPrices(ManncoStore, new PriceWithSupply
                    {
                        Price = available > 0 && price > 0 ? item.Currency.CalculateExchange(price, usdCurrency) : 0,
                        Supply = available
                    });
                }
            }

            var missingItems = dbItems.Where(x => !manncoStoreItems.Any(y => x.Name == y.Name) && x.Item.BuyPrices.ContainsKey(ManncoStore));
            foreach (var missingItem in missingItems)
            {
                missingItem.Item.UpdateBuyPrices(ManncoStore, null);
            }

            await _db.SaveChangesAsync();

            await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, ManncoStore, x =>
            {
                x.TotalItems = manncoStoreItems.Count();
                x.TotalListings = manncoStoreItems.Sum(x => x.Count);
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
                logger.LogError(ex, $"Failed to update market item price information from Mannco.store (appId: {app.SteamId}). {ex.Message}");
                await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, ManncoStore, x =>
                {
                    x.LastUpdateErrorOn = DateTimeOffset.Now;
                    x.LastUpdateError = ex.Message;
                });
            }
            catch (Exception)
            {
                logger.LogError(ex, $"Failed to update market item price statistics for Mannco.store (appId: {app.SteamId}). {ex.Message}");
            }
        }
    }
}
