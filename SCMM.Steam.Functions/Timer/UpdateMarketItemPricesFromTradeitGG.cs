using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.TradeitGG.Client;
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

public class UpdateMarketItemPricesFromTradeitGG
{
    private readonly SteamDbContext _db;
    private readonly TradeitGGWebClient _tradeitGGWebClient;
    private readonly IStatisticsService _statisticsService;

    public UpdateMarketItemPricesFromTradeitGG(SteamDbContext db, TradeitGGWebClient tradeitGGWebClient, IStatisticsService statisticsService)
    {
        _db = db;
        _tradeitGGWebClient = tradeitGGWebClient;
        _statisticsService = statisticsService;
    }

    [Function("Update-Market-Item-Prices-From-TradeitGG")]
    public async Task Run([TimerTrigger("0 6/30 * * * *")] /* every 30mins */ TimerInfo timerInfo, FunctionContext context)
    {
        if (!MarketType.TradeitGG.IsEnabled())
        {
            return;
        }

        var logger = context.GetLogger("Update-Market-Item-Prices-From-TradeitGG");
        var stopwatch = new Stopwatch();

        var appIds = MarketType.TradeitGG.GetSupportedAppIds().Select(x => x.ToString()).ToArray();
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
            logger.LogTrace($"Updating item price information from Tradeit.gg (appId: {app.SteamId})");
            var statisticsKey = String.Format(StatisticKeys.MarketStatusByAppId, app.SteamId);

            try
            {
                stopwatch.Restart();
                var tradeitGGItems = new Dictionary<TradeitGGItem, int>();
                var inventoryDataItems = (IDictionary<TradeitGGItem, int>)null;
                var inventoryDataOffset = 0;
                var inventoryDataLimit = TradeitGGWebClient.MaxPageLimit;
                do
                {
                    // NOTE: Items have to be fetched in multiple batches, keep reading until no new items are found
                    inventoryDataItems = await _tradeitGGWebClient.GetNewInventoryDataAsync(app.SteamId, offset: inventoryDataOffset, limit: inventoryDataLimit);
                    if (inventoryDataItems?.Any() == true)
                    {
                        tradeitGGItems.AddRange(inventoryDataItems);
                        inventoryDataOffset += inventoryDataLimit;
                    }
                } while (inventoryDataItems?.Any() == true && inventoryDataItems?.Count == inventoryDataLimit);

                var items = await _db.SteamMarketItems
                    .Where(x => x.AppId == app.Id)
                    .Select(x => new
                    {
                        Name = x.Description.NameHash,
                        Currency = x.Currency,
                        Item = x,
                    })
                    .ToListAsync();

                foreach (var tradeitGGItem in tradeitGGItems)
                {
                    var item = items.FirstOrDefault(x => x.Name == tradeitGGItem.Key.Name)?.Item;
                    if (item != null)
                    {
                        item.UpdateBuyPrices(MarketType.TradeitGG, new PriceWithSupply
                        {
                            Price = tradeitGGItem.Value > 0 ? item.Currency.CalculateExchange(tradeitGGItem.Key.Price, usdCurrency) : 0,
                            Supply = tradeitGGItem.Value
                        });
                    }
                }

                var missingItems = items.Where(x => !tradeitGGItems.Any(y => x.Name == y.Key.Name) && x.Item.BuyPrices.ContainsKey(MarketType.TradeitGG));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.TradeitGG, null);
                }

                await _db.SaveChangesAsync();

                await _statisticsService.UpdateDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, MarketType.TradeitGG, x =>
                {
                    x.TotalItems = tradeitGGItems.Count();
                    x.TotalListings = tradeitGGItems.Sum(i => i.Value);
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
                    logger.LogError(ex, $"Failed to update market item price information from Tradeit.gg (appId: {app.SteamId}). {ex.Message}");
                    await _statisticsService.UpdateDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, MarketType.TradeitGG, x =>
                    {
                        x.LastUpdateErrorOn = DateTimeOffset.Now;
                        x.LastUpdateError = ex.Message;
                    });
                }
                catch (Exception)
                {
                    logger.LogError(ex, $"Failed to update market item price statistics for Tradeit.gg (appId: {app.SteamId}). {ex.Message}");
                }
            }
            finally
            {
                stopwatch.Stop();
            }
        }
    }
}
