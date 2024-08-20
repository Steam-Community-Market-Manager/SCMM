using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.Rustyloot.Client;
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

public class UpdateMarketItemPricesFromRustyloot
{
    private const MarketType Rustyloot = MarketType.Rustyloot;

    private readonly SteamDbContext _db;
    private readonly RustylootWebClient _rustylootWebClient;
    private readonly IStatisticsService _statisticsService;

    public UpdateMarketItemPricesFromRustyloot(SteamDbContext db, RustylootWebClient rustylootWebClient, IStatisticsService statisticsService)
    {
        _db = db;
        _rustylootWebClient = rustylootWebClient;
        _statisticsService = statisticsService;
    }

    [Function("Update-Market-Item-Prices-From-Rustyloot")]
    public async Task Run([TimerTrigger("0 5/30 * * * *")] /* every 30mins */ TimerInfo timerInfo, FunctionContext context)
    {
        if (!Rustyloot.IsEnabled())
        {
            return;
        }

        var logger = context.GetLogger("Update-Market-Item-Prices-From-Rustyloot");

        var appIds = Rustyloot.GetSupportedAppIds().Select(x => x.ToString()).ToArray();
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
            logger.LogTrace($"Updating market item price information from Rustyloot (appId: {app.SteamId})");
            await UpdateRustylootPricesForApp(logger, app, usdCurrency);
        }
    }

    private async Task UpdateRustylootPricesForApp(ILogger logger, SteamApp app, SteamCurrency usdCurrency)
    {
        var statisticsKey = String.Format(StatisticKeys.MarketStatusByAppId, app.SteamId);
        var stopwatch = new Stopwatch();
        try
        {
            stopwatch.Start();

            var rustylootItems = await _rustylootWebClient.GetSiteInventory();

            var dbItems = await _db.SteamMarketItems
                .Where(x => x.AppId == app.Id)
                .Select(x => new
                {
                    Name = x.Description.NameHash,
                    Currency = x.Currency,
                    Item = x,
                })
                .ToListAsync();

            var rustylootItemGroups = rustylootItems.Where(x => !(x.Flagged > 0) && !(x.Locked > 0)).GroupBy(x => x.Name);
            foreach (var rustylootItemGroup in rustylootItemGroups)
            {
                var item = dbItems.FirstOrDefault(x => x.Name == rustylootItemGroup.Key)?.Item;
                if (item != null)
                {
                    var lowestHousePrice = rustylootItemGroup.Min(x => x.Price);
                    var normalisedHousePrice = lowestHousePrice > 0 ? (long)(Math.Round((decimal)lowestHousePrice / 1000, 2) * 100) : 0; // Round and remove 1 digit of precision to normalise with the Steam price format
                    var normalisedUsdPrice = usdCurrency.CalculateExchange(normalisedHousePrice, Rustyloot.GetCheapestBuyOption()?.GetHouseCurrency()); // Convert from coins to USD
                    var supply = rustylootItemGroup.Sum(x => x.Amount);
                    item.UpdateBuyPrices(Rustyloot, new PriceWithSupply
                    {
                        Price = supply > 0 ? item.Currency.CalculateExchange(normalisedUsdPrice, usdCurrency) : 0,
                        Supply = supply
                    });
                }
            }

            var missingItems = dbItems.Where(x => !rustylootItems.Any(y => x.Name == y.Name) && x.Item.BuyPrices.ContainsKey(Rustyloot));
            foreach (var missingItem in missingItems)
            {
                missingItem.Item.UpdateBuyPrices(Rustyloot, null);
            }

            await _db.SaveChangesAsync();

            await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, Rustyloot, x =>
            {
                x.TotalItems = rustylootItemGroups.Count();
                x.TotalListings = rustylootItemGroups.Sum(x => x.Count());
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
                logger.LogError(ex, $"Failed to update market item price information from Rustyloot (appId: {app.SteamId}, source: exchange). {ex.Message}");
                await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, Rustyloot, x =>
                {
                    x.LastUpdateErrorOn = DateTimeOffset.Now;
                    x.LastUpdateError = ex.Message;
                });
            }
            catch (Exception)
            {
                logger.LogError(ex, $"Failed to update market item price statistics for Rustyloot (appId: {app.SteamId}, exchange). {ex.Message}");
            }
        }
    }
}
