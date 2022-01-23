﻿using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.RustTM.Client;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemPricesFromRustTMJob
{
    private readonly SteamDbContext _db;
    private readonly RustTMWebClient _rustTMWebClient;

    public UpdateMarketItemPricesFromRustTMJob(SteamDbContext db, RustTMWebClient rustTMWebClient)
    {
        _db = db;
        _rustTMWebClient = rustTMWebClient;
    }

    [Function("Update-Market-Item-Prices-From-RustTM")]
    public async Task Run([TimerTrigger("0 8-59/15 * * * *")] /* every 15mins */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Prices-From-RustTM");

        var steamApps = await _db.SteamApps
            .Where(x => x.IsActive)
            .Where(x => x.SteamId == Constants.RustAppId.ToString())
            .ToListAsync();
        if (!steamApps.Any())
        {
            return;
        }

        // Prices are returned in USD by default
        var usdCurrency = _db.SteamCurrencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyUSD);
        if (usdCurrency == null)
        {
            return;
        }

        foreach (var app in steamApps)
        {
            logger.LogTrace($"Updating item price information from Rust.tm (appId: {app.SteamId})");
            var items = await _db.SteamMarketItems
                .Where(x => x.AppId == app.Id)
                .Select(x => new
                {
                    Name = x.Description.NameHash,
                    Currency = x.Currency,
                    Item = x,
                })
                .ToListAsync();

            try
            {
                var rustTMItems = await _rustTMWebClient.GetPricesAsync(usdCurrency.Name);
                foreach (var rustTMItem in rustTMItems)
                {
                    var item = items.FirstOrDefault(x => x.Name == rustTMItem.MarketHashName)?.Item;
                    if (item != null)
                    {
                        item.UpdateBuyPrices(MarketType.RustTM, new PriceWithSupply
                        {
                            Price = rustTMItem.Volume > 0 ? item.Currency.CalculateExchange(rustTMItem.Price.ToString().SteamPriceAsInt(), usdCurrency) : 0,
                            Supply = rustTMItem.Volume
                        });
                    }
                }

                var missingItems = items.Where(x => !rustTMItems.Any(y => x.Name == y.MarketHashName) && x.Item.BuyPrices.ContainsKey(MarketType.RustTM));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.RustTM, null);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item price information from Rust.tm (appId: {app.SteamId}). {ex.Message}");
                continue;
            }

            _db.SaveChanges();
        }
    }
}