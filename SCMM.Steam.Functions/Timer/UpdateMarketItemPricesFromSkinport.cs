using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.Skinport.Client;
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

public class UpdateMarketItemPricesFromSkinport
{
    private const MarketType Skinport = MarketType.Skinport;

    private readonly SteamDbContext _db;
    private readonly SkinportWebClient _skinportWebClient;
    private readonly IStatisticsService _statisticsService;

    public UpdateMarketItemPricesFromSkinport(SteamDbContext db, SkinportWebClient skinportWebClient, IStatisticsService statisticsService)
    {
        _db = db;
        _skinportWebClient = skinportWebClient;
        _statisticsService = statisticsService;
    }

    [Function("Update-Market-Item-Prices-From-Skinport")]
    public async Task Run([TimerTrigger("0 0/30 * * * *")] /* every 30mins */ TimerInfo timerInfo, FunctionContext context)
    {
        if (!Skinport.IsEnabled())
        {
            return;
        }

        var logger = context.GetLogger("Update-Market-Item-Prices-From-Skinport");
        var stopwatch = new Stopwatch();

        var appIds = Skinport.GetSupportedAppIds().Select(x => x.ToString()).ToArray();
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
            logger.LogTrace($"Updating market item price information from Skinport (appId: {app.SteamId})");
            var statisticsKey = String.Format(StatisticKeys.MarketStatusByAppId, app.SteamId);

            try
            {
                stopwatch.Restart();
                var skinportItems = (await _skinportWebClient.GetItemsAsync(app.SteamId, currency: usdCurrency.Name, tradable: true)) ?? new List<SkinportItem>();

                var items = await _db.SteamMarketItems
                   .Where(x => x.AppId == app.Id)
                   .Select(x => new
                   {
                       Name = x.Description.NameHash,
                       Currency = x.Currency,
                       Item = x,
                   })
                   .ToListAsync();

                foreach (var skinportItem in skinportItems)
                {
                    var item = items.FirstOrDefault(x => x.Name == skinportItem.MarketHashName)?.Item;
                    if (item != null)
                    {
                        item.UpdateBuyPrices(Skinport, new PriceWithSupply
                        {
                            Price = skinportItem.Quantity > 0 ? item.Currency.CalculateExchange((skinportItem.MinPrice ?? skinportItem.SuggestedPrice).ToString().SteamPriceAsInt(), usdCurrency) : 0,
                            Supply = skinportItem.Quantity
                        });
                    }
                }

                var missingItems = items.Where(x => !skinportItems.Any(y => x.Name == y.MarketHashName) && x.Item.BuyPrices.ContainsKey(Skinport));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(Skinport, null);
                }

                await _db.SaveChangesAsync();

                await _statisticsService.UpdateDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, Skinport, x =>
                {
                    x.TotalItems = skinportItems.Count();
                    x.TotalListings = skinportItems.Sum(i => i.Quantity);
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
                    logger.LogError(ex, $"Failed to update market item price information from Skinport (appId: {app.SteamId}). {ex.Message}");
                    await _statisticsService.UpdateDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, Skinport, x =>
                    {
                        x.LastUpdateErrorOn = DateTimeOffset.Now;
                        x.LastUpdateError = ex.Message;
                    });
                }
                catch (Exception)
                {
                    logger.LogError(ex, $"Failed to update market item price statistics for Skinport (appId: {app.SteamId}). {ex.Message}");
                }
                continue;
            }
            finally
            {
                stopwatch.Stop();
            }
        }
    }
}
