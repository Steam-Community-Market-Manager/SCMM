using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.SkinBaron.Client;
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

public class UpdateMarketItemPricesFromSkinBaron
{
#pragma warning disable CS0618 // Type or member is obsolete
    private const MarketType SkinBaron = MarketType.SkinBaron;
#pragma warning restore CS0618 // Type or member is obsolete

    private readonly SteamDbContext _db;
    private readonly SkinBaronWebClient _skinBaronWebClient;
    private readonly IStatisticsService _statisticsService;

    public UpdateMarketItemPricesFromSkinBaron(SteamDbContext db, SkinBaronWebClient skinBaronWebClient, IStatisticsService statisticsService)
    {
        _db = db;
        _skinBaronWebClient = skinBaronWebClient;
        _statisticsService = statisticsService;
    }

    [Function("Update-Market-Item-Prices-From-SkinBaron")]
    public async Task Run([TimerTrigger("0 10/30 * * * *")] /* every 30mins */ TimerInfo timerInfo, FunctionContext context)
    {
        if (!SkinBaron.IsEnabled())
        {
            return;
        }

        var logger = context.GetLogger("Update-Market-Item-Prices-From-SkinBaron");

        var appIds = SkinBaron.GetSupportedAppIds().Select(x => x.ToString()).ToArray();
        var supportedSteamApps = await _db.SteamApps
            .Where(x => appIds.Contains(x.SteamId))
            .ToListAsync();
        if (!supportedSteamApps.Any())
        {
            return;
        }

        // Prices are returned in EUR by default
        var eurCurrency = _db.SteamCurrencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyEUR);
        if (eurCurrency == null)
        {
            return;
        }

        foreach (var app in supportedSteamApps)
        {
            logger.LogTrace($"Updating item price information from SkinBaron (appId: {app.SteamId})");
            await UpdateSkinBaronMarketPricesForApp(logger, app, eurCurrency);
        }
    }

    private async Task UpdateSkinBaronMarketPricesForApp(ILogger logger, SteamApp app, SteamCurrency eurCurrency)
    {
        var statisticsKey = String.Format(StatisticKeys.MarketStatusByAppId, app.SteamId);
        var stopwatch = new Stopwatch();
        try
        {
            stopwatch.Start();

            var skinBaronItems = new List<SkinBaronItemOffer>();
            var offersResponse = (SkinBaronFilterOffersResponse)null;
            var browsingPage = 1;
            do
            {
                // NOTE: Items must be fetched across multiple pages, we keep reading until no new items/pages are found
                offersResponse = await _skinBaronWebClient.GetBrowsingFilterOffersAsync(app.SteamId, browsingPage);
                if (offersResponse?.AggregatedMetaOffers?.Any() == true)
                {
                    skinBaronItems.AddRange(offersResponse.AggregatedMetaOffers);
                    browsingPage++;
                }
            } while (offersResponse != null && offersResponse.ItemsPerPage > 0);

            var dbItems = await _db.SteamMarketItems
                .Where(x => x.AppId == app.Id)
                .Select(x => new
                {
                    Name = x.Description.NameHash,
                    Currency = x.Currency,
                    Item = x,
                })
                .ToListAsync();

            foreach (var skinBaronItem in skinBaronItems)
            {
                var item = dbItems.FirstOrDefault(x => x.Name == skinBaronItem.ExtendedProductInformation?.LocalizedName)?.Item;
                if (item != null)
                {
                    item.UpdateBuyPrices(SkinBaron, new PriceWithSupply
                    {
                        Price = skinBaronItem.NumberOfOffers > 0 ? item.Currency.CalculateExchange(skinBaronItem.LowestPrice.ToString().SteamPriceAsInt(), eurCurrency) : 0,
                        Supply = skinBaronItem.NumberOfOffers
                    });
                }
            }

            var missingItems = dbItems.Where(x => !skinBaronItems.Any(y => x.Name == y.ExtendedProductInformation?.LocalizedName) && x.Item.BuyPrices.ContainsKey(SkinBaron));
            foreach (var missingItem in missingItems)
            {
                missingItem.Item.UpdateBuyPrices(SkinBaron, null);
            }

            await _db.SaveChangesAsync();

            await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, SkinBaron, x =>
            {
                x.TotalItems = skinBaronItems.Count();
                x.TotalListings = skinBaronItems.Sum(i => i.NumberOfOffers);
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
                logger.LogError(ex, $"Failed to update market item price information from SkinBaron (appId: {app.SteamId}). {ex.Message}");
                await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, SkinBaron, x =>
                {
                    x.LastUpdateErrorOn = DateTimeOffset.Now;
                    x.LastUpdateError = ex.Message;
                });
            }
            catch (Exception)
            {
                logger.LogError(ex, $"Failed to update market item price statistics for SkinBaron (appId: {app.SteamId}). {ex.Message}");
            }
        }
    }
}
