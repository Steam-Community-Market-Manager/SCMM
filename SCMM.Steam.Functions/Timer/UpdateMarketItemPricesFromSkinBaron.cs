using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.SkinBaron.Client;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemPricesFromSkinBaron
{
    private readonly SteamDbContext _db;
    private readonly SkinBaronWebClient _skinBaronWebClient;

    public UpdateMarketItemPricesFromSkinBaron(SteamDbContext db, SkinBaronWebClient skinBaronWebClient)
    {
        _db = db;
        _skinBaronWebClient = skinBaronWebClient;
    }

    [Function("Update-Market-Item-Prices-From-SkinBaron")]
    public async Task Run([TimerTrigger("0 9-59/20 * * * *")] /* every 20mins */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Prices-From-SkinBaron");

        var supportedSteamApps = await _db.SteamApps
            .Where(x => x.SteamId == Constants.CSGOAppId.ToString() || x.SteamId == Constants.RustAppId.ToString())
            .Where(x => x.IsActive)
            .ToListAsync();
        if (!supportedSteamApps.Any())
        {
            return;
        }

        // Prices are returned in EUR by default
        var eurCurrency = _db.SteamCurrencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyEUR);
        if (eurCurrency == null)
        {
            return;
        }

        foreach (var app in supportedSteamApps)
        {
            logger.LogTrace($"Updating item price information from SkinBaron (appId: {app.SteamId})");

            try
            {
                var skinBaronItems = new List<SkinBaronItemOffer>();
                var offersResponse = (SkinBaronFilterOffersResponse)null;
                var browsingPage = 1;
                do
                {
                    // NOTE: Items have to be fetched in multiple pages, keep reading until no new items are found
                    // TODO: Needs optimisation, too slow, too many requests (429)
                    offersResponse = await _skinBaronWebClient.GetBrowsingFilterOffersAsync(app.SteamId, browsingPage);
                    if (offersResponse?.AggregatedMetaOffers?.Any() == true)
                    {
                        skinBaronItems.AddRange(offersResponse.AggregatedMetaOffers);
                        browsingPage++;
                    }
                } while (offersResponse != null && offersResponse.ItemsPerPage > 0);

                var items = await _db.SteamMarketItems
                    .Where(x => x.AppId == app.Id)
                    .Select(x => new
                    {
                        Name = x.Description.NameHash,
                        Currency = x.Currency,
                        Item = x,
                    })
                    .ToListAsync();

                foreach (var skinBaronItem in skinBaronItems)
                {
                    var item = items.FirstOrDefault(x => x.Name == skinBaronItem.ExtendedProductInformation?.LocalizedName)?.Item;
                    if (item != null)
                    {
                        item.UpdateBuyPrices(MarketType.SkinBaron, new PriceWithSupply
                        {
                            Price = skinBaronItem.NumberOfOffers > 0 ? item.Currency.CalculateExchange(skinBaronItem.LowestPrice.ToString().SteamPriceAsInt(), eurCurrency) : 0,
                            Supply = skinBaronItem.NumberOfOffers
                        });
                    }
                }

                var missingItems = items.Where(x => !skinBaronItems.Any(y => x.Name == y.ExtendedProductInformation?.LocalizedName) && x.Item.BuyPrices.ContainsKey(MarketType.SkinBaron));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.SkinBaron, null);
                    missingItem.Item.UpdateBuyPrices(MarketType.SkinBaron, null);
                }

                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item price information from SkinBaron (appId: {app.SteamId}). {ex.Message}");
                continue;
            }
        }
    }
}
