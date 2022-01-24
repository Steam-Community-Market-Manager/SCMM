using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.DMarket.Client;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemPricesFromDMarketJob
{
    private readonly SteamDbContext _db;
    private readonly DMarketWebClient _dMarketWebClient;

    public UpdateMarketItemPricesFromDMarketJob(SteamDbContext db, DMarketWebClient dMarketWebClient)
    {
        _db = db;
        _dMarketWebClient = dMarketWebClient;
    }

    [Function("Update-Market-Item-Prices-From-DMarket")]
    public async Task Run([TimerTrigger("0 30 * * * *")] /* every hour at 30mins past */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Prices-From-DMarket");

        var steamApps = await _db.SteamApps.Where(x => x.IsActive).ToListAsync();
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
            logger.LogTrace($"Updating market item price information from DMarket (appId: {app.SteamId})");
            
            try
            {
                var dMarketItems = new List<DMarketItem>();
                var marketItemsResponse = (DMarketMarketItemsResponse)null;
                do
                {
                    // NOTE: Items have to be fetched in multiple pages, keep reading until no new items are found
                    marketItemsResponse = await _dMarketWebClient.GetMarketItemsAsync(
                        app.Name, marketType: DMarketWebClient.MarketTypeDMarket, currencyName: usdCurrency.Name, cursor: marketItemsResponse?.Cursor, limit: DMarketWebClient.MaxPageLimit
                    );
                    if (marketItemsResponse?.Objects?.Any() == true)
                    {
                        dMarketItems.AddRange(marketItemsResponse.Objects);
                    }
                } while (marketItemsResponse != null && !String.IsNullOrEmpty(marketItemsResponse.Cursor));
                
                var items = await _db.SteamMarketItems
                    .Where(x => x.AppId == app.Id)
                    .Select(x => new
                    {
                        Name = x.Description.NameHash,
                        Currency = x.Currency,
                        Item = x,
                    })
                    .ToListAsync();

                foreach (var dMarketInventoryItemGroup in dMarketItems.GroupBy(x => x.Title))
                {
                    var item = items.FirstOrDefault(x => x.Name == dMarketInventoryItemGroup.Key)?.Item;
                    if (item != null)
                    {
                        var supply = dMarketInventoryItemGroup.Sum(x => x.Amount);
                        item.UpdateBuyPrices(MarketType.Dmarket, new PriceWithSupply
                        {
                            Price = supply > 0 ? item.Currency.CalculateExchange(dMarketInventoryItemGroup.Select(x => !String.IsNullOrEmpty(x.Price[usdCurrency.Name]) ? Int64.Parse(x.Price[usdCurrency.Name]) : 0).Min(x => x), usdCurrency) : 0,
                            Supply = supply
                        });
                    }
                }

                var missingItems = items.Where(x => !dMarketItems.Any(y => x.Name == y.Title) && x.Item.BuyPrices.ContainsKey(MarketType.Dmarket));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.Dmarket, null);
                }

                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item price information from DMarket (appId: {app.SteamId}). {ex.Message}");
                continue;
            }
        }
    }
}
