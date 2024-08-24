using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.DMarket.Client;
using SCMM.Shared.Abstractions.Statistics;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Models.Statistics;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemPricesFromDMarketJob
{
    private const MarketType DMarket = MarketType.DMarket;

    private readonly SteamDbContext _db;
    private readonly DMarketWebClient _dMarketWebClient;
    private readonly IStatisticsService _statisticsService;

    public UpdateMarketItemPricesFromDMarketJob(SteamDbContext db, DMarketWebClient dMarketWebClient, IStatisticsService statisticsService)
    {
        _db = db;
        _dMarketWebClient = dMarketWebClient;
        _statisticsService = statisticsService;
    }

    [Function("Update-Market-Item-Prices-From-DMarket")]
    public async Task Run([TimerTrigger("0 3/30 * * * *")] /* every 30mins */ TimerInfo timerInfo, FunctionContext context)
    {
        if (!DMarket.IsEnabled())
        {
            return;
        }

        var logger = context.GetLogger("Update-Market-Item-Prices-From-DMarket");

        var appIds = DMarket.GetSupportedAppIds().Select(x => x.ToString()).ToArray();
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
            logger.LogTrace($"Updating market item price information from DMarket (appId: {app.SteamId})");
            await UpdateDMarketPricesForApp(logger, app, usdCurrency);
        }
    }

    private async Task UpdateDMarketPricesForApp(ILogger logger, SteamApp app, SteamCurrency usdCurrency)
    {
        var statisticsKey = String.Format(StatisticKeys.MarketStatusByAppId, app.SteamId);
        var stopwatch = new Stopwatch();
        try
        {
            stopwatch.Start();

            var dMarketItems = await GetDMarketItemsAsync(logger, app, usdCurrency, DMarketWebClient.MarketTypeDMarket);
            var dbItems = await _db.SteamMarketItems
                .Where(x => x.AppId == app.Id)
                .Select(x => new
                {
                    Name = x.Description.NameHash,
                    Currency = x.Currency,
                    Item = x,
                })
                .ToListAsync();

            var dMarketItemGroups = dMarketItems.Where(x => x.Extra == null || (x.Extra.Tradable && !x.Extra.SaleRestricted)).GroupBy(x => x.Title);
            foreach (var dMarketInventoryItemGroup in dMarketItemGroups)
            {
                var item = dbItems.FirstOrDefault(x => x.Name == dMarketInventoryItemGroup.Key)?.Item;
                if (item != null)
                {
                    var supply = dMarketInventoryItemGroup.Sum(x => x.Amount);
                    item.UpdateBuyPrices(DMarket, new PriceWithSupply
                    {
                        Price = supply > 0 ? item.Currency.CalculateExchange(dMarketInventoryItemGroup.Select(x => !String.IsNullOrEmpty(x.Price[usdCurrency.Name]) ? Int64.Parse(x.Price[usdCurrency.Name]) : 0).Min(x => x), usdCurrency) : 0,
                        Supply = supply
                    });
                }
            }

            var missingItems = dbItems.Where(x => !dMarketItems.Any(y => x.Name == y.Title) && x.Item.BuyPrices.ContainsKey(DMarket));
            foreach (var missingItem in missingItems)
            {
                missingItem.Item.UpdateBuyPrices(DMarket, null);
            }

            await _db.SaveChangesAsync();

            await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, DMarket, x =>
            {
                x.TotalItems = dMarketItemGroups.Count();
                x.TotalListings = dMarketItemGroups.Sum(x => x.Sum(y => y.Amount));
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
                logger.LogError(ex, $"Failed to update market item price information from DMarket (appId: {app.SteamId}, source: exchange). {ex.Message}");
                await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, DMarket, x =>
                {
                    x.LastUpdateErrorOn = DateTimeOffset.Now;
                    x.LastUpdateError = ex.Message;
                });
            }
            catch (Exception)
            {
                logger.LogError(ex, $"Failed to update market item price statistics for DMarket (appId: {app.SteamId}, exchange). {ex.Message}");
            }
        }
    }

    private async Task<DMarketItem[]> GetDMarketItemsAsync(ILogger logger, SteamApp app, SteamCurrency usdCurrency, string marketType)
    {
        var dMarketItemDictionary = new ConcurrentDictionary<string, DMarketItem>();
        try
        {
            // NOTE: Brace yourself, this gets a bit wacky...
            //       DMarket APIs limit us to 100 items per page and pages cannot be fetched manually using an offset or page number (boo!).
            //       So what we do here is attack the item list from both ends using two threads. One thread starts from ascending order, the other starts from descending order.
            //       Once the two threads meet in the middle then we should have captured all the items in 50% of the time.
            var sortOrders = new[] { true, false };
            await Parallel.ForEachAsync(sortOrders, async (sortOrder, cancellationToken) =>
            {
                var newItemWasFound = false;
                var dMarketItemsResponse = (DMarketMarketItemsResponse)null;
                do
                {
                    // Reset. If we don't end up adding at least one new item in this page call, then we're probably overlapping with the other thread.
                    newItemWasFound = false;

                    // TODO: Optimise this if/when the API allows it, 100 items per read is too slow...
                    // NOTE: Items must be fetched across multiple pages, we keep reading until the next page cursor is empty
                    dMarketItemsResponse = await _dMarketWebClient.GetMarketItemsAsync(
                        app.Name, marketType: marketType, orderDescending: sortOrder, currencyName: usdCurrency.Name, cursor: dMarketItemsResponse?.Cursor, limit: DMarketWebClient.MaxPageLimit
                    );
                    if (dMarketItemsResponse?.Objects?.Any() == true)
                    {
                        foreach (var item in dMarketItemsResponse.Objects)
                        {
                            if (!dMarketItemDictionary.ContainsKey(item.ItemId))
                            {
                                dMarketItemDictionary[item.ItemId] = item;
                                newItemWasFound = true; // we found a new item, so keep going!
                            }
                        }
                    }

                } while (newItemWasFound && dMarketItemsResponse != null && !String.IsNullOrEmpty(dMarketItemsResponse.Cursor));
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to fetch all market items from DMarket (appId: {app.SteamId}, source: {marketType}). Request failed after fetching {dMarketItemDictionary.Count} items.");
        }

        return dMarketItemDictionary.Select(x => x.Value).ToArray();
    }

}
