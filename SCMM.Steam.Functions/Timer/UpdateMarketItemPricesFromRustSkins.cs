﻿using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.RustSkins.Client;
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

public class UpdateMarketItemPricesFromRustSkins
{
    private const MarketType RustSkins = MarketType.RustSkins;

    private readonly SteamDbContext _db;
    private readonly RustSkinsWebClient _rustSkinsWebClient;
    private readonly IStatisticsService _statisticsService;

    public UpdateMarketItemPricesFromRustSkins(SteamDbContext db, RustSkinsWebClient rustSkinsWebClient, IStatisticsService statisticsService)
    {
        _db = db;
        _rustSkinsWebClient = rustSkinsWebClient;
        _statisticsService = statisticsService;
    }

    [Function("Update-Market-Item-Prices-From-Rust-Skins")]
    public async Task Run([TimerTrigger("* * * * * *")] /* every 20mins */ TimerInfo timerInfo, FunctionContext context)
    {
        if (!RustSkins.IsEnabled())
        {
            return;
        }

        var logger = context.GetLogger("Update-Market-Item-Prices-From-Rust-Skins");

        var appIds = RustSkins.GetSupportedAppIds().Select(x => x.ToString()).ToArray();
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
            logger.LogTrace($"Updating item price information from RustSkins (appId: {app.SteamId})");
            await UpdateRustSkinsMarketPricesForApp(logger, app, usdCurrency);
        }
    }

    private async Task UpdateRustSkinsMarketPricesForApp(ILogger logger, SteamApp app, SteamCurrency usdCurrency)
    {
        var statisticsKey = String.Format(StatisticKeys.MarketStatusByAppId, app.SteamId);
        var stopwatch = new Stopwatch();
        try
        {
            stopwatch.Start();

            var rustSkinsItems = await _rustSkinsWebClient.GetMarketplaceData();
            var dbItems = await _db.SteamMarketItems
                .Where(x => x.AppId == app.Id)
                .Select(x => new
                {
                    Name = x.Description.NameHash,
                    Currency = x.Currency,
                    Item = x,
                })
                .ToListAsync();

            foreach (var rustSkinItem in rustSkinsItems)
            {
                var item = dbItems.FirstOrDefault(x => x.Name == rustSkinItem.Item)?.Item;
                if (item != null)
                {
                    item.UpdateBuyPrices(RustSkins, new PriceWithSupply
                    {
                        Price = rustSkinItem.Count > 0 ? item.Currency.CalculateExchange(rustSkinItem.Price.ToString().SteamPriceAsInt(), usdCurrency) : 0,
                        Supply = rustSkinItem.Count
                    });
                }
            }

            var missingItems = dbItems.Where(x => !rustSkinsItems.Any(y => x.Name == y.Item) && x.Item.BuyPrices.ContainsKey(RustSkins));
            foreach (var missingItem in missingItems)
            {
                missingItem.Item.UpdateBuyPrices(RustSkins, null);
            }

            await _db.SaveChangesAsync();

            await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, RustSkins, x =>
            {
                x.TotalItems = rustSkinsItems.Count();
                x.TotalListings = rustSkinsItems.Sum(i => i.Count);
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
                logger.LogError(ex, $"Failed to update market item price information from RustSkins (appId: {app.SteamId}). {ex.Message}");
                await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, RustSkins, x =>
                {
                    x.LastUpdateErrorOn = DateTimeOffset.Now;
                    x.LastUpdateError = ex.Message;
                });
            }
            catch (Exception)
            {
                logger.LogError(ex, $"Failed to update market item price statistics for RustSkins (appId: {app.SteamId}). {ex.Message}");
            }
        }
    }
}
