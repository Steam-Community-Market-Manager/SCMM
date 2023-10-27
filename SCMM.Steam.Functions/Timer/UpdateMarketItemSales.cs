﻿using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Client;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemSales
{
    private readonly SteamDbContext _db;
    private readonly ProxiedSteamCommunityWebClient _steamCommunityWebClient;
    private readonly IWebProxyManager _webProxyManager;

    private const int MarketItemBatchSize = 30;
    private readonly TimeSpan MarketItemMinimumAgeSinceLastUpdate = TimeSpan.FromHours(1);

    public UpdateMarketItemSales(SteamDbContext db, ProxiedSteamCommunityWebClient steamCommunityWebClient, IWebProxyManager webProxyManager)
    {
        _db = db;
        _steamCommunityWebClient = steamCommunityWebClient;
        _webProxyManager = webProxyManager;
    }

    [Function("Update-Market-Item-Sales")]
    public async Task Run([TimerTrigger("45 * * * * *")] /* 45 seconds past every minute */ TimerInfo timerInfo, FunctionContext context)
    {
        var jobId = Guid.NewGuid();
        var logger = context.GetLogger("Update-Market-Item-Sales");

        // Check that there are enough web proxies available to handle this batch of SCM requests, otherwise we cannot run
        var availableProxies = _webProxyManager.GetAvailableProxyCount(new Uri(Constants.SteamCommunityUrl));
        if (availableProxies < MarketItemBatchSize)
        {
            logger.LogWarning($"Update of market item sales information will be skipped as there are not enough available web proxies to handle our requests (proxies: {availableProxies}/{MarketItemBatchSize})");
            return;
        }
        else
        {
            logger.LogInformation($"Updating market item sales information (id: {jobId})");
        }

        // Find the next batch of items to be updated
        var cutoff = DateTimeOffset.Now.Subtract(MarketItemMinimumAgeSinceLastUpdate);
        var items = _db.SteamMarketItems
            .AsNoTracking()
            .Where(x => x.Description.IsMarketable)
            .Where(x => x.LastCheckedSalesOn == null || x.LastCheckedSalesOn <= cutoff)
            .Where(x => x.App.IsActive)
            .OrderBy(x => x.LastCheckedSalesOn)
            .Take(MarketItemBatchSize)
            .Select(x => new
            {
                Id = x.Id,
                AppId = x.App.SteamId,
                MarketHashName = x.Description.NameHash
            })
            .ToArray();
        if (!items.Any())
        {
            return;
        }

        // We assume all pricing data is in USD
        var usdCurrency = _db.SteamCurrencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyUSD);
        if (usdCurrency == null)
        {
            return;
        }

        // Ignore Steam data which has not changed recently
        _steamCommunityWebClient.IfModifiedSinceTimeAgo = MarketItemMinimumAgeSinceLastUpdate;

        // Fetch item data from steam in parallel (for better performance)
        var itemResponseMappings = new ConcurrentDictionary<Guid, string>();
        await Parallel.ForEachAsync(items, async (item, cancellationToken) =>
        {
            try
            {
                itemResponseMappings[item.Id] = await _steamCommunityWebClient.GetText(
                     new SteamMarketListingPageRequest()
                     {
                         AppId = item.AppId,
                         MarketHashName = item.MarketHashName,
                     }
                );
            }
            catch (SteamRequestException ex)
            {
                logger.LogError(ex, $"Failed to update market item sales history for '{item.MarketHashName}' ({item.Id}). {ex.Message}");
            }
            catch (SteamNotModifiedException ex)
            {
                logger.LogDebug(ex, $"No change in market item sales history for '{item.MarketHashName}' ({item.Id}) since last request. {ex.Message}");
            }
        });

        // Parse the responses and update the item prices
        if (itemResponseMappings.Any())
        {
            await UpdateMarketItemSalesHistory(itemResponseMappings);
        }

        logger.LogInformation($"Updated {itemResponseMappings.Count} market item sales information (id: {jobId})");
    }

    private async Task UpdateMarketItemSalesHistory(IDictionary<Guid, string> itemResponses)
    {
        var itemIds = itemResponses.Keys.ToArray();
        var items = await _db.SteamMarketItems
            .Where(x => itemIds.Contains(x.Id))
            .Include(x => x.App)
            .Include(x => x.Currency)
            .Include(x => x.Description)
            .Include(x => x.SalesHistory)
            .AsSplitQuery()
            .ToArrayAsync();

        Parallel.ForEach(items, (item) =>
        {
            var salesHistoryGraphJson = Regex.Match(itemResponses[item.Id], @"var line1=\[(.*)\];").Groups.OfType<Capture>().LastOrDefault()?.Value;
            if (!string.IsNullOrEmpty(salesHistoryGraphJson))
            {
                var salesHistoryGraph = JsonSerializer.Deserialize<string[][]>($"[{salesHistoryGraphJson}]");
                if (salesHistoryGraph != null)
                {
                    item.LastCheckedSalesOn = DateTimeOffset.Now;
                    item.RecalculateSales(
                        ParseMarketItemSalesFromGraph(salesHistoryGraph)
                    );
                }
            }
        });

        await _db.SaveChangesAsync();
    }

    private SteamMarketItemSale[] ParseMarketItemSalesFromGraph(string[][] salesGraph)
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
