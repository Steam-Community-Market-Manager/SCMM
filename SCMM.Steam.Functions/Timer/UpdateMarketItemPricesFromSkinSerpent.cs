using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.SkinSerpent.Client;
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

public class UpdateMarketItemPricesFromSkinSerpent
{
    private const MarketType SkinSerpent = MarketType.SkinSerpent;

    private readonly SteamDbContext _db;
    private readonly SkinSerpentWebClient _skinSerpentWebClient;
    private readonly IStatisticsService _statisticsService;

    public UpdateMarketItemPricesFromSkinSerpent(SteamDbContext db, SkinSerpentWebClient skinSerpentWebClient, IStatisticsService statisticsService)
    {
        _db = db;
        _skinSerpentWebClient = skinSerpentWebClient;
        _statisticsService = statisticsService;
    }

    [Function("Update-Market-Item-Prices-From-SkinSerpent")]
    public async Task Run([TimerTrigger("0 14/30 * * * *")] /* every 30mins */ TimerInfo timerInfo, FunctionContext context)
    {
        if (!SkinSerpent.IsEnabled())
        {
            return;
        }

        var logger = context.GetLogger("Update-Market-Item-Prices-From-SkinSerpent");

        var appIds = SkinSerpent.GetSupportedAppIds().Select(x => x.ToString()).ToArray();
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
            logger.LogTrace($"Updating market item price information from SkinSerpent (appId: {app.SteamId})");
            await UpdateSkinSerpentPricesForApp(logger, app, usdCurrency);
        }
    }

    private async Task UpdateSkinSerpentPricesForApp(ILogger logger, SteamApp app, SteamCurrency usdCurrency)
    {
        var statisticsKey = String.Format(StatisticKeys.MarketStatusByAppId, app.SteamId);
        var stopwatch = new Stopwatch();
        try
        {
            stopwatch.Start();

            var page = 0;
            var skinSerpentItems = new List<SkinSerpentListing>();
            var skinSerpentItemsResponse = (SkinSerpentListingsResponse)null;
            do
            {
                skinSerpentItemsResponse = await _skinSerpentWebClient.GetListingsAsync(app.SteamId, page: page);
                if (skinSerpentItemsResponse != null)
                {
                    if (skinSerpentItemsResponse.Listings?.Any() == true)
                    {
                        skinSerpentItems.AddRange(skinSerpentItemsResponse.Listings);
                    }
                    if (!String.IsNullOrEmpty(skinSerpentItemsResponse.NextPage))
                    {
                        page++;
                    }
                }
            } while (!String.IsNullOrEmpty(skinSerpentItemsResponse?.NextPage));

            var dbItems = await _db.SteamMarketItems
                .Where(x => x.AppId == app.Id)
                .Select(x => new
                {
                    Name = x.Description.NameHash,
                    Currency = x.Currency,
                    Item = x,
                })
                .ToListAsync();

            var skinSerpentItemGroups = skinSerpentItems.Where(x => x.Active && x.Skin != null).GroupBy(x => x.Skin.MarketHashName ?? x.Skin.MarketName ?? x.Skin.Name);
            foreach (var skinSerpentItemGroup in skinSerpentItemGroups)
            {
                var item = dbItems.FirstOrDefault(x => x.Name == skinSerpentItemGroup.Key)?.Item;
                if (item != null)
                {
                    var supply = skinSerpentItemGroup.Count();
                    item.UpdateBuyPrices(SkinSerpent, new PriceWithSupply
                    {
                        Price = supply > 0 ? item.Currency.CalculateExchange(skinSerpentItemGroup.Min(x => x.Price), usdCurrency) : 0,
                        Supply = supply
                    });
                }
            }

            var missingItems = dbItems.Where(x => !skinSerpentItems.Any(y => x.Name == (y.Skin?.MarketHashName ?? y.Skin?.MarketName ?? y.Skin?.Name)) && x.Item.BuyPrices.ContainsKey(SkinSerpent));
            foreach (var missingItem in missingItems)
            {
                missingItem.Item.UpdateBuyPrices(SkinSerpent, null);
            }

            await _db.SaveChangesAsync();

            await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, SkinSerpent, x =>
            {
                x.TotalItems = skinSerpentItemGroups.Count();
                x.TotalListings = skinSerpentItemGroups.Sum(x => x.Count());
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
                logger.LogError(ex, $"Failed to update market item price information from SkinSerpent (appId: {app.SteamId}, source: exchange). {ex.Message}");
                await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, SkinSerpent, x =>
                {
                    x.LastUpdateErrorOn = DateTimeOffset.Now;
                    x.LastUpdateError = ex.Message;
                });
            }
            catch (Exception)
            {
                logger.LogError(ex, $"Failed to update market item price statistics for SkinSerpent (appId: {app.SteamId}, exchange). {ex.Message}");
            }
        }
    }
}
