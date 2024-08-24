using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.SnipeSkins.Client;
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

public class UpdateMarketItemPricesFromSnipeSkins
{
    private const MarketType SnipeSkins = MarketType.SnipeSkins;

    private readonly SteamDbContext _db;
    private readonly SnipeSkinsWebClient _snipeSkinsWebClient;
    private readonly IStatisticsService _statisticsService;

    public UpdateMarketItemPricesFromSnipeSkins(SteamDbContext db, SnipeSkinsWebClient snipeSkinsWebClient, IStatisticsService statisticsService)
    {
        _db = db;
        _snipeSkinsWebClient = snipeSkinsWebClient;
        _statisticsService = statisticsService;
    }

    [Function("Update-Market-Item-Prices-From-SnipeSkins")]
    public async Task Run([TimerTrigger("0 17/30 * * * *")] /* every 30mins */ TimerInfo timerInfo, FunctionContext context)
    {
        if (!SnipeSkins.IsEnabled())
        {
            return;
        }

        var logger = context.GetLogger("Update-Market-Item-Prices-From-SnipeSkins");

        var appIds = SnipeSkins.GetSupportedAppIds().Select(x => x.ToString()).ToArray();
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
            logger.LogTrace($"Updating market item price information from SnipeSkins (appId: {app.SteamId})");
            await UpdateSnipeSkinsMarketPricesForApp(logger, app, usdCurrency);
        }
    }

    private async Task UpdateSnipeSkinsMarketPricesForApp(ILogger logger, SteamApp app, SteamCurrency cnyCurrency)
    {
        var statisticsKey = String.Format(StatisticKeys.MarketStatusByAppId, app.SteamId);
        var stopwatch = new Stopwatch();
        try
        {
            stopwatch.Start();

            var snipeSkinsItems = await _snipeSkinsWebClient.GetPricesAsync(app.SteamId);
            var dbItems = await _db.SteamMarketItems
                .Where(x => x.AppId == app.Id)
                .Select(x => new
                {
                    Name = x.Description.NameHash,
                    Currency = x.Currency,
                    Item = x,
                })
                .ToListAsync();

            foreach (var snipeSkinsItem in snipeSkinsItems)
            {
                var item = dbItems.FirstOrDefault(x => x.Name == snipeSkinsItem.MarketHashName)?.Item;
                if (item != null)
                {
                    item.UpdateBuyPrices(SnipeSkins, new PriceWithSupply
                    {
                        Price = snipeSkinsItem.Quantity > 0 ? item.Currency.CalculateExchange(snipeSkinsItem.LowestMarketPrice, cnyCurrency) : 0,
                        Supply = snipeSkinsItem.Quantity
                    });
                }
            }

            var missingItems = dbItems.Where(x => !snipeSkinsItems.Any(y => x.Name == y.MarketHashName) && x.Item.BuyPrices.ContainsKey(SnipeSkins));
            foreach (var missingItem in missingItems)
            {
                missingItem.Item.UpdateBuyPrices(SnipeSkins, null);
            }

            await _db.SaveChangesAsync();

            await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, SnipeSkins, x =>
            {
                x.TotalItems = snipeSkinsItems.Count();
                x.TotalListings = snipeSkinsItems.Sum(x => x.Quantity);
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
                logger.LogError(ex, $"Failed to update market item price information from SnipeSkins (appId: {app.SteamId}). {ex.Message}");
                await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, SnipeSkins, x =>
                {
                    x.LastUpdateErrorOn = DateTimeOffset.Now;
                    x.LastUpdateError = ex.Message;
                });
            }
            catch (Exception)
            {
                logger.LogError(ex, $"Failed to update market item price statistics for SnipeSkins (appId: {app.SteamId}). {ex.Message}");
            }
        }
    }
}
