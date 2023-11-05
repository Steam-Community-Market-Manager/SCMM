﻿using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.CSDeals.Client;
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

public class UpdateMarketItemPricesFromCSDeals
{
    private const MarketType CSDealsMarketplace = MarketType.CSDealsMarketplace;
    private const MarketType CSDealsTrade = MarketType.CSDealsTrade;

    private readonly SteamDbContext _db;
    private readonly CSDealsWebClient _csDealsWebClient;
    private readonly IStatisticsService _statisticsService;

    public UpdateMarketItemPricesFromCSDeals(SteamDbContext db, CSDealsWebClient csDealsWebClient, IStatisticsService statisticsService)
    {
        _db = db;
        _csDealsWebClient = csDealsWebClient;
        _statisticsService = statisticsService;
    }

    [Function("Update-Market-Item-Prices-From-CSDeals")]
    public async Task Run([TimerTrigger("0 8/30 * * * *")] /* every 30mins */ TimerInfo timerInfo, FunctionContext context)
    {
        if (!CSDealsTrade.IsEnabled())
        {
            return;
        }

        var logger = context.GetLogger("Update-Market-Item-Prices-From-CSDeals");

        var appIds = CSDealsTrade.GetSupportedAppIds().Select(x => x.ToString()).ToArray();
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
            logger.LogTrace($"Updating market item price information from CS.Deals (appId: {app.SteamId})");
            await UpdateCSDealsTradePricesForApp(logger, app, usdCurrency);
            await UpdateCsDealsMarketplacePricesForApp(logger, app, usdCurrency);
        }
    }

    private async Task UpdateCSDealsTradePricesForApp(ILogger logger, SteamApp app, SteamCurrency usdCurrency)
    {
        var statisticsKey = String.Format(StatisticKeys.MarketStatusByAppId, app.SteamId);
        var stopwatch = new Stopwatch();
        try
        {
            stopwatch.Start();

            var csDealsInventoryItems = (await _csDealsWebClient.PostBotsInventoryAsync(app.SteamId))?.Items?.FirstOrDefault(x => x.Key == app.SteamId).Value ?? new CSDealsItemListing[0];
            var dbItems = await _db.SteamMarketItems
                .Where(x => x.AppId == app.Id)
                .Select(x => new
                {
                    Name = x.Description.NameHash,
                    Currency = x.Currency,
                    Item = x,
                })
                .ToListAsync();

            var csDealsInventoryItemGroups = csDealsInventoryItems.GroupBy(x => x.MarketName);
            foreach (var csDealsInventoryItemGroup in csDealsInventoryItemGroups)
            {
                var item = dbItems.FirstOrDefault(x => x.Name == csDealsInventoryItemGroup.Key)?.Item;
                if (item != null)
                {
                    var supply = csDealsInventoryItemGroup.Sum(x => x.ItemIds?.Length ?? 0);
                    item.UpdateBuyPrices(CSDealsTrade, new PriceWithSupply
                    {
                        Price = supply > 0 ? item.Currency.CalculateExchange((csDealsInventoryItemGroup.Min(x => x.ListingPrice)).ToString().SteamPriceAsInt(), usdCurrency) : 0,
                        Supply = supply
                    });
                }
            }

            var missingItems = dbItems.Where(x => !csDealsInventoryItems.Any(y => x.Name == y.MarketName) && x.Item.BuyPrices.ContainsKey(CSDealsTrade));
            foreach (var missingItem in missingItems)
            {
                missingItem.Item.UpdateBuyPrices(CSDealsTrade, null);
            }

            await _db.SaveChangesAsync();

            await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, CSDealsTrade, x =>
            {
                x.TotalItems = csDealsInventoryItemGroups.Count();
                x.TotalListings = csDealsInventoryItemGroups.Sum(x => x.Sum(y => y.ItemIds?.Length ?? 0));
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
                logger.LogError(ex, $"Failed to update trade item price information from CS.Deals (appId: {app.SteamId}, source: trade inventory). {ex.Message}");
                await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, CSDealsTrade, x =>
                {
                    x.LastUpdateErrorOn = DateTimeOffset.Now;
                    x.LastUpdateError = ex.Message;
                });
            }
            catch (Exception)
            {
                logger.LogError(ex, $"Failed to update trade item price statistics for CS.Deals (appId: {app.SteamId}, source: trade inventory). {ex.Message}");
            }
        }
    }

    private async Task UpdateCsDealsMarketplacePricesForApp(ILogger logger, SteamApp app, SteamCurrency usdCurrency)
    {
        var statisticsKey = String.Format(StatisticKeys.MarketStatusByAppId, app.SteamId);
        var stopwatch = new Stopwatch();
        try
        {
            stopwatch.Start();

            var csDealsLowestPriceItems = (await _csDealsWebClient.GetPricingGetLowestPricesAsync(app.SteamId)) ?? new List<CSDealsItemPrice>();
            var dbItems = await _db.SteamMarketItems
                .Where(x => x.AppId == app.Id)
                .Select(x => new
                {
                    Name = x.Description.NameHash,
                    Currency = x.Currency,
                    Item = x,
                })
                .ToListAsync();

            foreach (var csDealsLowestPriceItem in csDealsLowestPriceItems)
            {
                var item = dbItems.FirstOrDefault(x => x.Name == csDealsLowestPriceItem.MarketName)?.Item;
                if (item != null)
                {
                    item.UpdateBuyPrices(CSDealsMarketplace, new PriceWithSupply
                    {
                        Price = item.Currency.CalculateExchange(csDealsLowestPriceItem.LowestPrice.SteamPriceAsInt(), usdCurrency),
                        Supply = null
                    });
                }
            }

            var missingItems = dbItems.Where(x => !csDealsLowestPriceItems.Any(y => x.Name == y.MarketName) && x.Item.BuyPrices.ContainsKey(CSDealsMarketplace));
            foreach (var missingItem in missingItems)
            {
                missingItem.Item.UpdateBuyPrices(CSDealsMarketplace, null);
            }

            await _db.SaveChangesAsync();

            await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, CSDealsMarketplace, x =>
            {
                x.TotalItems = csDealsLowestPriceItems.Count();
                x.TotalListings = null;
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
                logger.LogError(ex, $"Failed to update market item price information from CS.Deals (appId: {app.SteamId}, source: lowest price items). {ex.Message}");
                await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, CSDealsMarketplace, x =>
                {
                    x.LastUpdateErrorOn = DateTimeOffset.Now;
                    x.LastUpdateError = ex.Message;
                });
            }
            catch (Exception)
            {
                logger.LogError(ex, $"Failed to update market item price statistics for CS.Deals (appId: {app.SteamId}, source: lowest price items). {ex.Message}");
            }
        }
    }
}
