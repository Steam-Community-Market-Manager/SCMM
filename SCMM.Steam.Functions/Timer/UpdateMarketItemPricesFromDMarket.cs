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
using System.Diagnostics;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemPricesFromDMarketJob
{
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
    public async Task Run([TimerTrigger("0 30 * * * *")] /* every hour at 30mins past */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Prices-From-DMarket");
        var stopwatch = new Stopwatch();

        var appIds = MarketType.Dmarket.GetSupportedAppIds().Select(x => x.ToString()).ToArray();
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
            logger.LogTrace($"Updating market item price information from DMarket (appId: {app.SteamId})");
            var statisticsKey = String.Format(StatisticKeys.MarketStatusByAppId, app.SteamId);

            try
            {
                stopwatch.Restart();
                var dMarketItems = new List<DMarketItem>();
                var marketItemsResponse = (DMarketMarketItemsResponse)null;
                do
                {
                    // TODO: Optimise this if/when the API allows it, 100 items per read is way too slow
                    // NOTE: Items have to be fetched in multiple pages, keep reading until no new items are found
                    marketItemsResponse = await _dMarketWebClient.GetMarketItemsAsync(
                        app.Name, marketType: DMarketWebClient.MarketTypeDMarket, currencyName: usdCurrency.Name, cursor: marketItemsResponse?.Cursor, limit: DMarketWebClient.MaxPageLimit
                    );
                    if (marketItemsResponse?.Objects?.Any() == true)
                    {
                        dMarketItems.AddRange(marketItemsResponse.Objects);
                    }
                } while (marketItemsResponse != null && !String.IsNullOrEmpty(marketItemsResponse.Cursor));

                var items = await _db.SteamMarketItems
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
                    var item = items.FirstOrDefault(x => x.Name == dMarketInventoryItemGroup.Key)?.Item;
                    if (item != null)
                    {
                        var supply = dMarketInventoryItemGroup.Sum(x => x.Amount);
                        item.UpdateBuyPrices(MarketType.Dmarket, new PriceWithSupply
                        {
                            Price = supply > 0 ? item.Currency.CalculateExchange(dMarketInventoryItemGroup.Select(x => !String.IsNullOrEmpty(x.Price[usdCurrency.Name]) ? Int64.Parse(x.Price[usdCurrency.Name]) : 0).Min(x => x), usdCurrency) : 0,
                            Supply = supply
                        });
                    }
                }

                var missingItems = items.Where(x => !dMarketItems.Any(y => x.Name == y.Title) && x.Item.BuyPrices.ContainsKey(MarketType.Dmarket));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.Dmarket, null);
                }

                await _db.SaveChangesAsync();

                await _statisticsService.UpdateDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, MarketType.Dmarket, x =>
                {
                    x.TotalItems = dMarketItemGroups.Count();
                    x.TotalListings = dMarketItemGroups.Sum(i => i.Sum(z => z.Amount));
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
                    logger.LogError(ex, $"Failed to update market item price information from DMarket (appId: {app.SteamId}). {ex.Message}");
                    await _statisticsService.UpdateDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, MarketType.Dmarket, x =>
                    {
                        x.LastUpdateErrorOn = DateTimeOffset.Now;
                        x.LastUpdateError = ex.Message;
                    });
                }
                catch (Exception)
                {
                    logger.LogError(ex, $"Failed to update market item price statistics for DMarket (appId: {app.SteamId}). {ex.Message}");
                }
            }
            finally
            {
                stopwatch.Stop();
            }
        }
    }
}
