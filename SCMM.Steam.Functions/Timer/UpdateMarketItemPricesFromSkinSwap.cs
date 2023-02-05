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
        var logger = context.GetLogger("Update-Market-Item-Prices-From-SkinSwap");
        var stopwatch = new Stopwatch();

        var appIds = MarketType.SkinSwap.GetSupportedAppIds().Select(x => x.ToString()).ToArray();
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
            logger.LogTrace($"Updating item price information from SkinSwap (appId: {app.SteamId})");
            var statisticsKey = String.Format(StatisticKeys.MarketStatusByAppId, app.SteamId);

            try
            {
                stopwatch.Restart();
                var skinSwapItems = new List<SkinSwapItem>();
                var inventoryItemsResponse = (IEnumerable<SkinSwapItem>)null;
                var offset = 0;
                do
                {
                    // TODO: Optimise this if/when the API allows it, 60 items per read is too slow
                    // NOTE: Items have to be fetched in multiple pages, keep reading until no new items are found
                    inventoryItemsResponse = await _skinSwapWebClient.GetInventoryAsync(app.SteamId, offset);
                    if (inventoryItemsResponse?.Any() == true)
                    {
                        skinSwapItems.AddRange(inventoryItemsResponse);
                    }
                    offset += (inventoryItemsResponse?.Count() ?? 0);
                } while (inventoryItemsResponse != null && inventoryItemsResponse.Count() > 0);

                var items = await _db.SteamMarketItems
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
                    var item = items.FirstOrDefault(x => x.Name == skinSwapItemGroup.Key)?.Item;
                    if (item != null)
                    {
                        var supply = skinSwapItemGroup.Sum(x => x.Amount);
                        item.UpdateBuyPrices(MarketType.SkinSwap, new PriceWithSupply
                        {
                            Price = supply > 0 ? item.Currency.CalculateExchange(skinSwapItemGroup.Min(x => x.TradePrice).ToString().SteamPriceAsInt(), usdCurrency) : 0,
                            Supply = supply
                        });
                    }
                }

                var missingItems = items.Where(x => !skinSwapItems.Any(y => x.Name == y.MarketHashName) && x.Item.BuyPrices.ContainsKey(MarketType.SkinSwap));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.SkinSwap, null);
                }

                await _db.SaveChangesAsync();

                await _statisticsService.UpdateDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, MarketType.SkinSwap, x =>
                {
                    x.TotalItems = skinSwapItemGroups.Count();
                    x.TotalListings = skinSwapItemGroups.Sum(i => i.Sum(z => z.Amount));
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
                    await _statisticsService.UpdateDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, MarketType.SkinSwap, x =>
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
        }
    }
}
