using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Web.Client;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.Community.Responses.Json;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using System.Collections.Concurrent;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemOrders
{
    private readonly SteamDbContext _db;
    private readonly ProxiedSteamCommunityWebClient _steamCommunityWebClient;
    private readonly IWebProxyManager _webProxyManager;

    private const int MarketItemBatchSize = 30;
    private readonly TimeSpan MarketItemMinimumAgeSinceLastUpdate = TimeSpan.FromHours(1);

    public UpdateMarketItemOrders(SteamDbContext db, ProxiedSteamCommunityWebClient steamCommunityWebClient, IWebProxyManager webProxyManager)
    {
        _db = db;
        _steamCommunityWebClient = steamCommunityWebClient;
        _webProxyManager = webProxyManager;
    }

    [Function("Update-Market-Item-Orders")]
    public async Task Run([TimerTrigger("15 * * * * *")] /* 15 seconds past every minute */ TimerInfo timerInfo, FunctionContext context)
    {
        var jobId = Guid.NewGuid();
        var logger = context.GetLogger("Update-Market-Item-Orders");

        // Check that there are enough web proxies available to handle this batch of SCM requests, otherwise we cannot run
        var availableProxies = _webProxyManager.GetAvailableProxyCount(new Uri(Constants.SteamCommunityUrl));
        if (availableProxies < MarketItemBatchSize)
        {
            logger.LogWarning($"Update of market item orders information will be skipped as there are not enough available web proxies to handle our requests (proxies: {availableProxies}/{MarketItemBatchSize})");
            return;
        }
        else
        {
            logger.LogInformation($"Updating market item orders information (id: {jobId})");
        }

        // Find the next batch of items to be updated
        var cutoff = DateTimeOffset.Now.Subtract(MarketItemMinimumAgeSinceLastUpdate);
        var items = _db.SteamMarketItems
            .AsNoTracking()
            .Where(x => !string.IsNullOrEmpty(x.SteamId))
            .Where(x => x.Description.IsMarketable)
            .Where(x => x.LastCheckedOrdersOn == null || x.LastCheckedOrdersOn <= cutoff)
            .Where(x => x.App.IsActive)
            .OrderBy(x => x.LastCheckedOrdersOn)
            .Take(MarketItemBatchSize)
            .Select(x => new
            {
                Id = x.Id,
                ItemNameId = x.SteamId,
                CurrencyId = x.Currency.SteamId,
                AppId = x.App.SteamId,
                MarketHashName = x.Description.NameHash
            })
            .ToArray();
        if (!items.Any())
        {
            return;
        }

        // Ignore Steam data which has not changed recently
        _steamCommunityWebClient.IfModifiedSinceTimeAgo = MarketItemMinimumAgeSinceLastUpdate;

        // Fetch item data from steam in parallel (for better performance)
        var itemResponseMappings = new ConcurrentDictionary<Guid, SteamMarketItemOrdersHistogramJsonResponse>();
        await Parallel.ForEachAsync(items, async (item, cancellationToken) =>
        {
            try
            {
                itemResponseMappings[item.Id] = await _steamCommunityWebClient.GetMarketItemOrdersHistogram(
                    new SteamMarketItemOrdersHistogramJsonRequest()
                    {
                        ItemNameId = item.ItemNameId,
                        Language = Constants.SteamDefaultLanguage,
                        CurrencyId = item.CurrencyId,
                        NoRender = true
                    },
                    item.AppId.ToString(),
                    item.MarketHashName
                );
            }
            catch (SteamRequestException ex)
            {
                logger.LogError(ex, $"Failed to update market item orders for '{item.MarketHashName}' ({item.Id}). {ex.Message}");
            }
            catch (SteamNotModifiedException ex)
            {
                logger.LogDebug(ex, $"No change in market item orders for '{item.MarketHashName}' ({item.Id}) since last request. {ex.Message}");
            }
        });

        // Parse the responses and update the item prices
        if (itemResponseMappings.Any())
        {
            await UpdateMarketItemOrderHistory(itemResponseMappings);
        }

        logger.LogInformation($"Updated {itemResponseMappings.Count} market item orders information (id: {jobId})");
    }

    private async Task UpdateMarketItemOrderHistory(IDictionary<Guid, SteamMarketItemOrdersHistogramJsonResponse> itemResponses)
    {
        var itemIds = itemResponses.Keys.ToArray();
        var items = await _db.SteamMarketItems
            .Where(x => itemIds.Contains(x.Id))
            .Include(x => x.App)
            .Include(x => x.Currency)
            .Include(x => x.Description)
            .Include(x => x.BuyOrders)
            .Include(x => x.SellOrders)
            .AsSplitQuery()
            .ToArrayAsync();

        Parallel.ForEach(items, (item) =>
        {
            var histogram = itemResponses[item.Id];
            item.LastCheckedOrdersOn = DateTimeOffset.Now;
            item.RecalculateOrders(
                ParseMarketItemOrdersFromGraph<SteamMarketItemBuyOrder>(histogram.BuyOrderGraph),
                histogram.BuyOrderCount.SteamQuantityValueAsInt(),
                ParseMarketItemOrdersFromGraph<SteamMarketItemSellOrder>(histogram.SellOrderGraph),
                histogram.SellOrderCount.SteamQuantityValueAsInt()
            );
        });

        await _db.SaveChangesAsync();
    }

    private T[] ParseMarketItemOrdersFromGraph<T>(string[][] orderGraph)
        where T : Steam.Data.Store.SteamMarketItemOrder, new()
    {
        var orders = new List<T>();
        if (orderGraph == null)
        {
            return orders.ToArray();
        }

        var totalQuantity = 0;
        for (var i = 0; i < orderGraph.Length; i++)
        {
            var price = orderGraph[i][0].SteamPriceAsInt();
            var quantity = (orderGraph[i][1].SteamQuantityValueAsInt() - totalQuantity);
            orders.Add(new T()
            {
                Price = price,
                Quantity = quantity,
            });
            totalQuantity += quantity;
        }

        return orders.ToArray();
    }
}
