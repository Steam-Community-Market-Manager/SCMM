using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.Skinport.Client;
using SCMM.Steam.API;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemThirdPartyPricesJob
{
    private readonly SteamDbContext _db;
    private readonly SkinportWebClient _skinportWebClient;

    public UpdateMarketItemThirdPartyPricesJob(SteamDbContext db, SkinportWebClient skinportWebClient)
    {
        _db = db;
        _skinportWebClient = skinportWebClient;
    }

    [Function("Update-Market-Item-Third-Party-Prices")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Admin, "get", "post")] HttpRequestData req, FunctionContext context)
    //public async Task Run([TimerTrigger("30 * * * * *")] /* every minute, at 30 seconds past */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Third-Party-Prices");

        var cutoff = DateTimeOffset.Now.Subtract(TimeSpan.FromHours(1));
        var items = _db.SteamMarketItems
            .Include(x => x.App)
            .Include(x => x.Currency)
            .Where(x => x.Description.IsMarketable && !string.IsNullOrEmpty(x.Description.NameHash))
            .Where(x => x.LastCheckedThirdPartyPricesOn == null || x.LastCheckedThirdPartyPricesOn <= cutoff)
            .OrderBy(x => x.LastCheckedThirdPartyPricesOn)
            .Take(10) // batch 10 at a time
            .ToList();

        if (!items.Any())
        {
            return null;
        }

        var id = Guid.NewGuid();
        logger.LogTrace($"Updating market item third party price information (id: {id}, count: {items.Count()})");
        foreach (var item in items)
        {
            try
            {
                item.Prices = new PersistablePriceStockDictionary(item.Prices);

                var skinportItems = await _skinportWebClient.BrowseMarketItemsAsync(item.App.SteamId, item.Description.NameHash);
                if (skinportItems.Any())
                {
                    var filterdItems = skinportItems
                        .Where(x => x.MarketHashName == item.Description.NameHash)
                        .Where(x => x.SaleStatus == SkinportMarketItem.SaleStatusListed)
                        .Where(x => x.SaleType == SkinportMarketItem.SaleTypePublic)
                        .ToList();
                    if (filterdItems.Any())
                    {
                        item.Prices[PriceType.Skinport] = new PriceStock
                        {
                            Price = filterdItems.Min(x => x.SalePrice),
                            Stock = filterdItems.Count()
                        };
                    }
                    else if (item.Prices.ContainsKey(PriceType.Skinport))
                    {
                        item.Prices.Remove(PriceType.Skinport);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item third party price for '{item.SteamId}'. {ex.Message}");
                continue;
            }
        }

        _db.SaveChanges();
        logger.LogTrace($"Updated market item third part price information (id: {id})");
        return null;
    }
}
