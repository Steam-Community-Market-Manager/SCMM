using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.Buff.Client;
using SCMM.Shared.Abstractions.Statistics;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Models.Statistics;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemPricesFromBuff
{
    private readonly SteamDbContext _db;
    private readonly BuffWebClient _buffWebClient;
    private readonly IStatisticsService _statisticsService;

    public UpdateMarketItemPricesFromBuff(SteamDbContext db, BuffWebClient buffWebClient, IStatisticsService statisticsService)
    {
        _db = db;
        _buffWebClient = buffWebClient;
        _statisticsService = statisticsService;
    }

    [Function("Update-Market-Item-Prices-From-Buff")]
    public async Task Run([TimerTrigger("0 1-59/20 * * * *")] /* every 20mins */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Prices-From-Buff");

        var appIds = MarketType.Buff.GetSupportedAppIds().Select(x => x.ToString()).ToArray();
        var supportedSteamApps = await _db.SteamApps
            .Where(x => appIds.Contains(x.SteamId))
            .Where(x => x.IsActive)
            .ToListAsync();
        if (!supportedSteamApps.Any())
        {
            return;
        }

        // Prices are returned in CNY by default
        var cnyCurrency = _db.SteamCurrencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyCNY);
        if (cnyCurrency == null)
        {
            return;
        }

        foreach (var app in supportedSteamApps)
        {
            logger.LogTrace($"Updating market item price information from Buff (appId: {app.SteamId})");
            var statisticsKey = String.Format(StatisticKeys.MarketStatusByAppId, app.SteamId);

            try
            {
                var buffItems = new List<BuffItem>();
                var marketGoodsResponse = (BuffMarketGoodsResponse)null;
                do
                {
                    // NOTE: Items have to be fetched in multiple pages, keep reading until no new items are found
                    marketGoodsResponse = await _buffWebClient.GetMarketGoodsAsync(app.Name, (marketGoodsResponse?.PageNum ?? 0) + 1);
                    if (marketGoodsResponse?.Items?.Any() == true)
                    {
                        buffItems.AddRange(marketGoodsResponse.Items);
                    }
                } while (marketGoodsResponse != null && marketGoodsResponse.PageNum < marketGoodsResponse.TotalPage);

                var items = await _db.SteamMarketItems
                    .Where(x => x.AppId == app.Id)
                    .Select(x => new
                    {
                        Name = x.Description.NameHash,
                        Currency = x.Currency,
                        Item = x,
                    })
                    .ToListAsync();

                foreach (var buffItem in buffItems)
                {
                    var item = items.FirstOrDefault(x => x.Name == buffItem.MarketHashName)?.Item;
                    if (item != null)
                    {
                        item.UpdateBuyPrices(MarketType.Buff, new PriceWithSupply
                        {
                            Price = buffItem.SellNum > 0 ? item.Currency.CalculateExchange(buffItem.SellMinPrice.SteamPriceAsInt(), cnyCurrency) : 0,
                            Supply = buffItem.SellNum
                        });
                    }
                }

                var missingItems = items.Where(x => !buffItems.Any(y => x.Name == y.MarketHashName) && x.Item.BuyPrices.ContainsKey(MarketType.Buff));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.Buff, null);
                }

                await _db.SaveChangesAsync();

                await _statisticsService.UpdateDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, MarketType.Buff, x =>
                {
                    x.TotalItems = buffItems.Count();
                    x.TotalListings = buffItems.Sum(i => i.SellNum);
                    x.LastUpdatedItemsOn = DateTimeOffset.Now;
                    x.LastUpdateErrorOn = null;
                    x.LastUpdateError = null;
                });
            }
            catch (Exception ex)
            {
                try
                {
                    logger.LogError(ex, $"Failed to update market item price information from Buff (appId: {app.SteamId}). {ex.Message}");
                    await _statisticsService.UpdateDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, MarketType.Buff, x =>
                    {
                        x.LastUpdateErrorOn = DateTimeOffset.Now;
                        x.LastUpdateError = ex.Message;
                    });
                }
                catch (Exception)
                {
                    logger.LogError(ex, $"Failed to update market item price statistics for Buff (appId: {app.SteamId}). {ex.Message}");
                }
            }
        }
    }
}
