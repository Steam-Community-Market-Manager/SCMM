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
        if (!MarketType.SkinBaron.IsEnabled())
        {
            return;
        }

        var logger = context.GetLogger("Update-Market-Item-Prices-From-SkinBaron");
        var stopwatch = new Stopwatch();

        var appIds = MarketType.SkinBaron.GetSupportedAppIds().Select(x => x.ToString()).ToArray();
        var supportedSteamApps = await _db.SteamApps
            .Where(x => appIds.Contains(x.SteamId))
            //.Where(x => x.IsActive)
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
            var statisticsKey = String.Format(StatisticKeys.MarketStatusByAppId, app.SteamId);

            try
            {
                stopwatch.Restart();
                var skinBaronItems = new List<SkinBaronItemOffer>();
                var offersResponse = (SkinBaronFilterOffersResponse)null;
                var browsingPage = 1;
                do
                {
                    // NOTE: Items have to be fetched in multiple pages, keep reading until no new items are found
                    // TODO: Needs optimisation, too slow, too many requests (429)
                    offersResponse = await _skinBaronWebClient.GetBrowsingFilterOffersAsync(app.SteamId, browsingPage);
                    if (offersResponse?.AggregatedMetaOffers?.Any() == true)
                    {
                        skinBaronItems.AddRange(offersResponse.AggregatedMetaOffers);
                        browsingPage++;
                    }
                } while (offersResponse != null && offersResponse.ItemsPerPage > 0);

                var items = await _db.SteamMarketItems
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
                    var item = items.FirstOrDefault(x => x.Name == skinBaronItem.ExtendedProductInformation?.LocalizedName)?.Item;
                    if (item != null)
                    {
                        item.UpdateBuyPrices(MarketType.SkinBaron, new PriceWithSupply
                        {
                            Price = skinBaronItem.NumberOfOffers > 0 ? item.Currency.CalculateExchange(skinBaronItem.LowestPrice.ToString().SteamPriceAsInt(), eurCurrency) : 0,
                            Supply = skinBaronItem.NumberOfOffers
                        });
                    }
                }

                var missingItems = items.Where(x => !skinBaronItems.Any(y => x.Name == y.ExtendedProductInformation?.LocalizedName) && x.Item.BuyPrices.ContainsKey(MarketType.SkinBaron));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.SkinBaron, null);
                    missingItem.Item.UpdateBuyPrices(MarketType.SkinBaron, null);
                }

                await _db.SaveChangesAsync();

                await _statisticsService.UpdateDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, MarketType.SkinBaron, x =>
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
                    await _statisticsService.UpdateDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, MarketType.SkinBaron, x =>
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
            finally
            {
                stopwatch.Stop();
            }
        }
    }
}
