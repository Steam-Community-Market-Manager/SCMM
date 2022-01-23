﻿using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.iTradegg.Client;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemPricesFromiTradeggJob
{
    private readonly SteamDbContext _db;
    private readonly iTradeggWebClient _iTradeggWebClient;

    public UpdateMarketItemPricesFromiTradeggJob(SteamDbContext db, iTradeggWebClient iTradeggWebClient)
    {
        _db = db;
        _iTradeggWebClient = iTradeggWebClient;
    }

    [Function("Update-Market-Item-Prices-From-iTradegg")]
    public async Task Run([TimerTrigger("0 5-59/15 * * * *")] /* every 15mins */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Prices-From-iTradegg");

        var steamApps = await _db.SteamApps
            .Where(x => x.IsActive)
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
            logger.LogTrace($"Updating item price information from iTrade.gg (appId: {app.SteamId})");
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
                var iTradeggItems = await _iTradeggWebClient.GetInventoryAsync(app.SteamId);
                foreach (var iTradeggItem in iTradeggItems)
                {
                    var item = items.FirstOrDefault(x => x.Name == iTradeggItem.Name)?.Item;
                    if (item != null)
                    {
                        item.UpdateBuyPrices(MarketType.iTradegg, new PriceWithSupply
                        {
                            Price = iTradeggItem.Same > 0 ? item.Currency.CalculateExchange(iTradeggItem.Price.ToString().SteamPriceAsInt(), usdCurrency) : 0,
                            Supply = iTradeggItem.Same
                        });
                    }
                }

                var missingItems = items.Where(x => !iTradeggItems.Any(y => x.Name == y.Name) && x.Item.BuyPrices.ContainsKey(MarketType.iTradegg));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.iTradegg, null);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item price information from iTrade.gg (appId: {app.SteamId}). {ex.Message}");
                continue;
            }

            _db.SaveChanges();
        }
    }
}