using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.RustSkins.Client;
using SCMM.Market.SkinBaron.Client;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemPricesFromRustSkinsJob
{
    private readonly SteamDbContext _db;
    private readonly RustSkinsWebClient _rustSkinsWebClient;

    public UpdateMarketItemPricesFromRustSkinsJob(SteamDbContext db, RustSkinsWebClient rustSkinsWebClient)
    {
        _db = db;
        _rustSkinsWebClient = rustSkinsWebClient;
    }

    [Function("Update-Market-Item-Prices-From-RustSkins")]
    public async Task Run([TimerTrigger("0 7-59/15 * * * *")] /* every 15mins */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Prices-From-RustSkins");

        var steamApps = await _db.SteamApps
            .Where(x => x.IsActive)
            .Where(x => x.SteamId == Constants.RustAppId.ToString())
            .ToListAsync();
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
            logger.LogTrace($"Updating item price information from RUSTSkins (appId: {app.SteamId})");
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
                var rustSkinsItems = new List<RustSkinsItemListing>();
                var listingsResponse = (RustSkinsMarketListingsResponse) null;
                var listingPage = 1;
                do
                {
                    // NOTE: Items have to be fetched in multiple pages, keep reading until no new items are found
                    listingsResponse = await _rustSkinsWebClient.GetMarketListingsAsync(listingPage);
                    if (listingsResponse.Success && listingsResponse.Listings?.Any() == true)
                    {
                        rustSkinsItems.AddRange(listingsResponse.Listings);
                        listingPage++;
                    }
                } while (listingsResponse.Success && listingsResponse.Listings?.Any() == true);
                if (rustSkinsItems?.Any() != true)
                {
                    continue;
                }

                foreach (var rustSkinItemGroups in rustSkinsItems.GroupBy(x => x.MarketHashName))
                {
                    var item = items.FirstOrDefault(x => x.Name == rustSkinItemGroups.Key)?.Item;
                    if (item != null)
                    {
                        var supply = rustSkinItemGroups.Count();
                        item.UpdateBuyPrices(MarketType.RUSTSkins, new PriceWithSupply
                        {
                            Price = supply > 0 ? item.Currency.CalculateExchange(rustSkinItemGroups.Min(x => x.CustomPrice).ToString().SteamPriceAsInt(), usdCurrency) : 0,
                            Supply = supply
                        });
                    }
                }

                var missingItems = items.Where(x => !rustSkinsItems.Any(y => x.Name == y.MarketHashName) && x.Item.BuyPrices.ContainsKey(MarketType.RUSTSkins));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.RUSTSkins, null);
                    missingItem.Item.UpdateBuyPrices(MarketType.RUSTSkins, null);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item price information from RUSTSkins (appId: {app.SteamId}). {ex.Message}");
                continue;
            }

            _db.SaveChanges();
        }
    }
}
