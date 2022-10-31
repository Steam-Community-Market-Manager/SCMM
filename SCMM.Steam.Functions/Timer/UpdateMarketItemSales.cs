using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.Community.Responses.Json;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using System.Globalization;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemSales
{
    private readonly SteamDbContext _db;
    private readonly SteamCommunityWebClient _steamCommunityWebClient;

    public UpdateMarketItemSales(SteamDbContext db, SteamCommunityWebClient steamCommunityWebClient)
    {
        _db = db;
        _steamCommunityWebClient = steamCommunityWebClient;
    }

    //[Function("Update-Market-Item-Sales")]
    public async Task Run([TimerTrigger("0 1-59/2 * * * *")] /* every odd minute */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Sales");

        var cutoff = DateTimeOffset.Now.Subtract(TimeSpan.FromHours(1));
        var items = _db.SteamMarketItems
            .Include(x => x.Currency)
            .Where(x => x.LastCheckedSalesOn == null || x.LastCheckedSalesOn <= cutoff)
            .OrderBy(x => x.LastCheckedSalesOn)
            .Include(x => x.App)
            .Include(x => x.Description)
            .Take(100) // batch 100 at a time
            .ToList();

        if (!items.Any())
        {
            return;
        }

        var nzdCurrency = _db.SteamCurrencies.FirstOrDefault(x => x.Name == "NZD");
        if (nzdCurrency == null)
        {
            return;
        }

        var id = Guid.NewGuid();
        logger.LogTrace($"Updating market item sales information (id: {id}, count: {items.Count()})");
        foreach (var item in items)
        {
            try
            {
                var response = await _steamCommunityWebClient.GetMarketPriceHistory(
                    new SteamMarketPriceHistoryJsonRequest()
                    {
                        AppId = item.App.SteamId,
                        MarketHashName = item.Description.Name,
                        //CurrencyId = item.Currency.SteamId
                    }
                );

                // HACK: Our Steam account is locked to NZD, we must convert all prices to the items currency
                // TODO: Find/buy a Steam account that is locked to USD for better accuracy
                await UpdateMarketItemSalesHistory(item, response, nzdCurrency);
            }
            catch (SteamRequestException ex)
            {
                logger.LogError(ex, $"Failed to update market item sales history for '{item.SteamId}'. {ex.Message}");
            }
            finally
            {
                item.LastCheckedSalesOn = DateTimeOffset.Now;
                await _db.SaveChangesAsync();
            }
        }

        logger.LogTrace($"Updated market item sales information (id: {id})");
    }

    private async Task<SteamMarketItem> UpdateMarketItemSalesHistory(SteamMarketItem item, SteamMarketPriceHistoryJsonResponse sales, SteamCurrency salesCurrency = null)
    {
        if (item == null || sales?.Success != true)
        {
            return item;
        }

        // Lazy-load sales history if missing, required for recalculation
        if (item.SalesHistory?.Any() != true)
        {
            item = await _db.SteamMarketItems
                .Include(x => x.SalesHistory)
                .SingleOrDefaultAsync(x => x.Id == item.Id);
        }

        // If the sales are not already in our items currency, exchange them now
        var itemSales = ParseMarketItemSalesFromGraph(sales.Prices, item.LastCheckedSalesOn);
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
