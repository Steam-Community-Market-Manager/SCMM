using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.Community.Responses.Json;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemOrders
{
    private readonly SteamDbContext _db;
    private readonly SteamCommunityWebClient _steamCommunityWebClient;

    public UpdateMarketItemOrders(SteamDbContext db, SteamCommunityWebClient steamCommunityWebClient)
    {
        _db = db;
        _steamCommunityWebClient = steamCommunityWebClient;
    }

    [Function("Update-Market-Item-Orders")]
    public async Task Run([TimerTrigger("0 */2 * * * *")] /* every even minute */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Orders");

        var cutoff = DateTimeOffset.Now.Subtract(TimeSpan.FromHours(1));
        var items = _db.SteamMarketItems
            .Include(x => x.Currency)
            .Where(x => !string.IsNullOrEmpty(x.SteamId))
            .Where(x => x.LastCheckedOrdersOn == null || x.LastCheckedOrdersOn <= cutoff)
            .OrderBy(x => x.LastCheckedOrdersOn)
            .Take(100) // batch 100 at a time
            .ToList();

        if (!items.Any())
        {
            return;
        }

        var id = Guid.NewGuid();
        logger.LogTrace($"Updating market item orders information (id: {id}, count: {items.Count()})");
        foreach (var item in items)
        {
            try
            {
                var response = await _steamCommunityWebClient.GetMarketItemOrdersHistogram(
                    new SteamMarketItemOrdersHistogramJsonRequest()
                    {
                        ItemNameId = item.SteamId,
                        Language = Constants.SteamDefaultLanguage,
                        CurrencyId = item.Currency.SteamId,
                        NoRender = true
                    }
                );

                await UpdateMarketItemOrderHistory(item, response);
                await _db.SaveChangesAsync();
            }
            catch (SteamRequestException ex)
            {
                // If we're throttled, cool-down and try again later...
                logger.LogError(ex, $"Failed to update market item sales history for '{item.SteamId}'. {ex.Message}");
                if (ex.IsRateLimited)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item order history for '{item.SteamId}'. {ex.Message}");
                continue;
            }
        }

        logger.LogTrace($"Updated market item orders information (id: {id})");
    }

    private async Task<SteamMarketItem> UpdateMarketItemOrderHistory(SteamMarketItem item, SteamMarketItemOrdersHistogramJsonResponse histogram)
    {
        if (item == null || histogram?.Success != true)
        {
            return item;
        }

        // Lazy-load buy/sell order history if missing, required for recalculation
        if (item.BuyOrders?.Any() != true || item.SellOrders?.Any() != true)// || item.OrdersHistory?.Any() != true)
        {
            item = await _db.SteamMarketItems
                .Include(x => x.BuyOrders)
                .Include(x => x.SellOrders)
                //.Include(x => x.OrdersHistory)
                .SingleOrDefaultAsync(x => x.Id == item.Id);
        }

        item.LastCheckedOrdersOn = DateTimeOffset.Now;
        item.RecalculateOrders(
            ParseMarketItemOrdersFromGraph<SteamMarketItemBuyOrder>(histogram.BuyOrderGraph),
            histogram.BuyOrderCount.SteamQuantityValueAsInt(),
            ParseMarketItemOrdersFromGraph<SteamMarketItemSellOrder>(histogram.SellOrderGraph),
            histogram.SellOrderCount.SteamQuantityValueAsInt()
        );

        return item;
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
