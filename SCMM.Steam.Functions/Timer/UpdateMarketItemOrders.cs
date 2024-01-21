using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Web.Client;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.Community.Responses.Json;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemOrders
{
    private readonly SteamDbContext _db;
    private readonly SteamCommunityWebClient _steamCommunityWebClient;
    private readonly IWebProxyManager _webProxyManager;

    // TODO: Make these configurable
    private const int MarketItemBatchSize = 50;
    private readonly TimeSpan MarketItemMinimumAgeSinceLastUpdate = TimeSpan.FromHours(2);

    public UpdateMarketItemOrders(SteamDbContext db, SteamCommunityWebClient steamCommunityWebClient, IWebProxyManager webProxyManager)
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
            throw new Exception($"Update of market item orders information cannot run as there are not enough available web proxies to handle the requests (proxies: {availableProxies}/{MarketItemBatchSize})");
        }

        logger.LogTrace($"Updating market item orders information (id: {jobId})");

        // Find the next batch of items to be updated
        var cutoff = DateTimeOffset.Now.Subtract(MarketItemMinimumAgeSinceLastUpdate);
        var items = _db.SteamMarketItems
            .AsNoTracking()
            .Where(x => !string.IsNullOrEmpty(x.SteamId))
            .Where(x => x.Description.IsMarketable)
            .Where(x => x.LastCheckedOrdersOn == null || x.LastCheckedOrdersOn <= cutoff)
            .Where(x => (x.App.FeatureFlags & SteamAppFeatureFlags.ItemMarketPriceTracking) != 0)
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
                itemResponseMappings[item.Id] = await _steamCommunityWebClient.GetMarketItemOrdersHistogramAsync(
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
                logger.LogTrace(ex, $"No change in market item orders for '{item.MarketHashName}' ({item.Id}) since last request. {ex.Message}");
            }
        });

        // Parse the responses and update the item prices
        if (itemResponseMappings.Any())
        {
            // NOTE: We need to update the database items one at a time, due to performance limitations.
            //       The SQL demand of loading the entire buy/sell order history for 50+ items at a time will easily trigger a SQL timeout.
            var stopwatch = new Stopwatch();
            foreach (var item in itemResponseMappings)
            {
                stopwatch.Restart();
                await UpdateMarketItemOrderHistory(item.Key, item.Value);
                logger.LogTrace($"Updated item orders for '{item.Key}' in {stopwatch.Elapsed.ToDurationString(zero: "less than a second")}");
            }
        }

        logger.LogTrace($"Updated {itemResponseMappings.Count} market item orders information (id: {jobId})");
    }

    private async Task UpdateMarketItemOrderHistory(Guid itemId, SteamMarketItemOrdersHistogramJsonResponse itemResponse)
    {
        // TODO: Optimise this SQL to load faster or remove the eager load of all buy/sell orders
        var item = await _db.SteamMarketItems
            .Where(x => x.Id == itemId)
            .Include(x => x.App)
            .Include(x => x.Currency)
            .Include(x => x.Description)
            .Include(x => x.BuyOrders)
            .Include(x => x.SellOrders)
            .AsSplitQuery()
            .SingleOrDefaultAsync();

        item.LastCheckedOrdersOn = DateTimeOffset.Now;
        item.RecalculateOrders(
            ParseMarketItemOrdersFromGraph<SteamMarketItemBuyOrder>(itemResponse.BuyOrderGraph),
            itemResponse.BuyOrderCount.SteamQuantityValueAsInt(),
            ParseMarketItemOrdersFromGraph<SteamMarketItemSellOrder>(itemResponse.SellOrderGraph),
            itemResponse.SellOrderCount.SteamQuantityValueAsInt()
        );

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
