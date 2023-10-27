﻿using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.LootFarm.Client;
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

public class UpdateMarketItemPricesFromLootFarmJob
{
    private const MarketType LOOTFarm = MarketType.LOOTFarm;

    private readonly SteamDbContext _db;
    private readonly LootFarmWebClient _lootFarmWebClient;
    private readonly IStatisticsService _statisticsService;

    public UpdateMarketItemPricesFromLootFarmJob(SteamDbContext db, LootFarmWebClient lootFarmWebClient, IStatisticsService statisticsService)
    {
        _db = db;
        _lootFarmWebClient = lootFarmWebClient;
        _statisticsService = statisticsService;
    }

    [Function("Update-Market-Item-Prices-From-LootFarm")]
    public async Task Run([TimerTrigger("0 2/30 * * * *")] /* every 30mins */ TimerInfo timerInfo, FunctionContext context)
    {
        if (!LOOTFarm.IsEnabled())
        {
            return;
        }

        var logger = context.GetLogger("Update-Market-Item-Prices-From-LootFarm");
        var stopwatch = new Stopwatch();

        var appIds = LOOTFarm.GetSupportedAppIds().Select(x => x.ToString()).ToArray();
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
            logger.LogTrace($"Updating market item price information from LOOT.Farm (appId: {app.SteamId})");
            var statisticsKey = String.Format(StatisticKeys.MarketStatusByAppId, app.SteamId);

            try
            {
                stopwatch.Restart();
                var lootFarmItems = (await _lootFarmWebClient.GetItemPricesAsync(app.Name)) ?? new List<LootFarmItemPrice>();

                var items = await _db.SteamMarketItems
                    .Where(x => x.AppId == app.Id)
                    .Select(x => new
                    {
                        Name = x.Description.NameHash,
                        Currency = x.Currency,
                        Item = x,
                    })
                    .ToListAsync();

                foreach (var lootFarmItem in lootFarmItems)
                {
                    var item = items.FirstOrDefault(x => x.Name == lootFarmItem.Name)?.Item;
                    if (item != null)
                    {
                        var available = lootFarmItem.Have;
                        item.UpdateBuyPrices(LOOTFarm, new PriceWithSupply
                        {
                            Price = available > 0 ? item.Currency.CalculateExchange(lootFarmItem.Price, usdCurrency) : 0,
                            Supply = available
                        });
                    }
                }

                var missingItems = items.Where(x => !lootFarmItems.Any(y => x.Name == y.Name) && x.Item.BuyPrices.ContainsKey(LOOTFarm));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(LOOTFarm, null);
                }

                await _db.SaveChangesAsync();

                await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, LOOTFarm, x =>
                {
                    x.TotalItems = lootFarmItems.Count();
                    x.TotalListings = lootFarmItems.Sum(i => i.Have);
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
                    logger.LogError(ex, $"Failed to update market item price information from LOOT.Farm (appId: {app.SteamId}). {ex.Message}");
                    await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, LOOTFarm, x =>
                    {
                        x.LastUpdateErrorOn = DateTimeOffset.Now;
                        x.LastUpdateError = ex.Message;
                    });
                }
                catch (Exception)
                {
                    logger.LogError(ex, $"Failed to update market item price statistics for LOOT.Farm (appId: {app.SteamId}). {ex.Message}");
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
