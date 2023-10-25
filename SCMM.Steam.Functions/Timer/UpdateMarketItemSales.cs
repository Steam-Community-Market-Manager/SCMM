﻿using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemSales
{
    private readonly SteamDbContext _db;
    private readonly ProxiedSteamCommunityWebClient _steamCommunityWebClient;

    public UpdateMarketItemSales(SteamDbContext db, ProxiedSteamCommunityWebClient steamCommunityWebClient)
    {
        _db = db;
        _steamCommunityWebClient = steamCommunityWebClient;
    }

    [Function("Update-Market-Item-Sales")]
    public async Task Run([TimerTrigger("45 * * * * *")] /* 45 seconds past every minute */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Sales");

        var cutoff = DateTimeOffset.Now.Subtract(TimeSpan.FromHours(1));
        var items = _db.SteamMarketItems
            .Include(x => x.App)
            .Include(x => x.Currency)
            .Include(x => x.Description)
            .Where(x => x.Description.IsMarketable)
            .Where(x => x.LastCheckedSalesOn == null || x.LastCheckedSalesOn <= cutoff)
            .Where(x => x.App.IsActive)
            .OrderBy(x => x.LastCheckedSalesOn)
            .Take(30) // batch 30 items per minute
            .ToList();

        if (!items.Any())
        {
            return;
        }

        var usdCurrency = _db.SteamCurrencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyUSD);
        if (usdCurrency == null)
        {
            return;
        }

        //_steamCommunityWebClient.IfModifiedSinceTimeAgo = TimeSpan.FromHours(1);

        var id = Guid.NewGuid();
        logger.LogTrace($"Updating market item sales information (id: {id}, count: {items.Count()})");
        foreach (var item in items)
        {
            try
            {
                var responseHtml = await _steamCommunityWebClient.GetText(
                     new SteamMarketListingPageRequest()
                     {
                         AppId = item.App.SteamId,
                         MarketHashName = item.Description.Name,
                     }
                );

                var salesHistoryGraphArray = Regex.Match(responseHtml, @"var line1=\[(.*)\];").Groups.OfType<Capture>().LastOrDefault()?.Value;
                if (!string.IsNullOrEmpty(salesHistoryGraphArray))
                {
                    var salesHistoryGraph = JsonSerializer.Deserialize<string[][]>($"[{salesHistoryGraphArray}]"); 
                    await UpdateMarketItemSalesHistory(item, salesHistoryGraph, usdCurrency);
                    logger.LogInformation($"Market item sales history updated for '{item.Description?.Name}' ({item.Description?.ClassId})");
                }
                else
                {
                    throw new Exception("Unable to find sales history graph data in response");
                }
            }
            catch (SteamRequestException ex)
            {
                logger.LogError(ex, $"Failed to update market item sales history for '{item.SteamId}'. {ex.Message}");
            }
            catch (SteamNotModifiedException ex)
            {
                logger.LogInformation(ex, $"No change in market item sales history for '{item.SteamId}' since last request. {ex.Message}");
            }
            finally
            {
                await _db.SaveChangesAsync();
            }
        }

        logger.LogTrace($"Updated market item sales information (id: {id})");
    }

    private async Task<SteamMarketItem> UpdateMarketItemSalesHistory(SteamMarketItem item, string[][] salesGraph, SteamCurrency salesCurrency = null)
    {
        if (item == null || salesGraph == null || salesGraph.Length == 0)
        {
            return item;
        }

        // Lazy-load sales history if missing, required for recalculation
        if (item.SalesHistory?.Any() != true)
        {
            item = await _db.SteamMarketItems
                .Include(x => x.SalesHistory)
                .AsSplitQuery()
                .SingleOrDefaultAsync(x => x.Id == item.Id);
        }

        // If the sales are not already in our items currency, exchange them now
        var itemSales = ParseMarketItemSalesFromGraph(salesGraph, item.LastCheckedSalesOn);
        if (itemSales != null && salesCurrency != null && salesCurrency.Id != item.CurrencyId)
        {
            foreach (var sale in itemSales)
            {
                sale.MedianPrice = item.Currency.CalculateExchange(sale.MedianPrice, salesCurrency);
            }
        }

        item.LastCheckedSalesOn = DateTimeOffset.Now;
        item.RecalculateSales(itemSales);

        return item;
    }

    private SteamMarketItemSale[] ParseMarketItemSalesFromGraph(string[][] salesGraph, DateTimeOffset? ignoreSalesBefore = null)
    {
        var sales = new List<SteamMarketItemSale>();
        if (salesGraph == null)
        {
            return sales.ToArray();
        }

        var totalQuantity = 0;
        for (var i = 0; i < salesGraph.Length; i++)
        {
            var timeStamp = DateTime.ParseExact(salesGraph[i][0], "MMM dd yyyy HH: z", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            var medianPrice = salesGraph[i][1].SteamPriceAsInt();
            var quantity = salesGraph[i][2].SteamQuantityValueAsInt();
            sales.Add(new SteamMarketItemSale()
            {
                Timestamp = timeStamp,
                MedianPrice = medianPrice,
                Quantity = quantity,
            });
            totalQuantity += quantity;
        }

        return sales.ToArray();
    }
}
