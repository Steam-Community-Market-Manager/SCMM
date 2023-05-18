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
    private readonly ProxiedSteamCommunityWebClient _steamCommunityWebClient;

    public UpdateMarketItemOrders(SteamDbContext db, ProxiedSteamCommunityWebClient steamCommunityWebClient)
    {
        _db = db;
        _steamCommunityWebClient = steamCommunityWebClient;
    }

    [Function("Update-Market-Item-Orders")]
    public async Task Run([TimerTrigger("15 * * * * *")] /* 15 seconds past every minute */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Orders");

        var cutoff = DateTimeOffset.Now.Subtract(TimeSpan.FromHours(1));
        var items = _db.SteamMarketItems
            .Include(x => x.App)
            .Include(x => x.Currency)
            .Include(x => x.Description)
            .Where(x => !string.IsNullOrEmpty(x.SteamId))
            .Where(x => x.Description.IsMarketable)
            .Where(x => x.LastCheckedOrdersOn == null || x.LastCheckedOrdersOn <= cutoff)
            .Where(x => x.App.IsActive)
            .OrderBy(x => x.LastCheckedOrdersOn)
            .Take(30) // batch 30 items per minute
            .ToList();

        if (!items.Any())
        {
            return;
        }

        //_steamCommunityWebClient.IfModifiedSinceTimeAgo = TimeSpan.FromHours(1);

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
                    },
                    item.App.SteamId.ToString(),
                    item.Description.NameHash
                );

                await UpdateMarketItemOrderHistory(item, response);
            }
            catch (SteamRequestException ex)
            {
                logger.LogError(ex, $"Failed to update market item orders for '{item.SteamId}'. {ex.Message}");
            }
            catch (SteamNotModifiedException ex)
            {
                logger.LogInformation(ex, $"No change in market item orders for '{item.SteamId}' since last request. {ex.Message}");
            }
            finally
            {
                await _db.SaveChangesAsync();
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
                .AsSplitQuery()
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
