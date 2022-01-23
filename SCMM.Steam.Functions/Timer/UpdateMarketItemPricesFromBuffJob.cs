using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.Buff.Client;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemPricesFromBuffJob
{
    private readonly SteamDbContext _db;
    private readonly BuffWebClient _buffWebClient;

    public UpdateMarketItemPricesFromBuffJob(SteamDbContext db, BuffWebClient buffWebClient)
    {
        _db = db;
        _buffWebClient = buffWebClient;
    }

    [Function("Update-Market-Item-Prices-From-Buff")]
    public async Task Run([TimerTrigger("0 1-59/15 * * * *")] /* every 15mins */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Prices-From-Buff");

        var steamApps = await _db.SteamApps.Where(x => x.IsActive).ToListAsync();
        if (!steamApps.Any())
        {
            return;
        }

        // Prices are returned in CNY by default
        var cnyCurrency = _db.SteamCurrencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyCNY);
        if (cnyCurrency == null)
        {
            return;
        }

        foreach (var app in steamApps)
        {
            logger.LogTrace($"Updating market item price information from Buff (appId: {app.SteamId})");
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
                var buffItems = new List<BuffItem>();
                var marketGoodsResponse = (BuffMarketGoodsResponse)null;
                do
                {
                    // NOTE: Items have to be fetched in multiple pages, keep reading until no new items are found
                    marketGoodsResponse = await _buffWebClient.GetMarketGoodsAsync(app.SteamId, (marketGoodsResponse?.PageNum ?? 0) + 1);
                    if (marketGoodsResponse.Items?.Any() == true)
                    {
                        buffItems.AddRange(marketGoodsResponse.Items);
                    }
                } while (marketGoodsResponse.PageNum < marketGoodsResponse.TotalPage);
                if (buffItems?.Any() != true)
                {
                    continue;
                }

                foreach (var buffItem in buffItems)
                {
                    var item = items.FirstOrDefault(x => x.Name == buffItem.MarketHashName)?.Item;
                    if (item != null)
                    {
                        item.UpdateBuyPrices(MarketType.Buff, new PriceWithSupply
                        {
                            Price = buffItem.SellNum > 0 ? item.Currency.CalculateExchange(buffItem.SellMinPrice.SteamPriceAsInt(), cnyCurrency) : 0,
                            Supply = buffItem.SellNum
                        });
                    }
                }

                var missingItems = items.Where(x => !buffItems.Any(y => x.Name == y.MarketHashName) && x.Item.BuyPrices.ContainsKey(MarketType.Buff));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.Buff, null);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item price information from Buff (appId: {app.SteamId}). {ex.Message}");
                continue;
            }

            _db.SaveChanges();
        }
    }
}
