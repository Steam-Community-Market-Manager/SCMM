using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.LootFarm.Client;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemPricesFromLootFarmtJob
{
    private readonly SteamDbContext _db;
    private readonly LootFarmWebClient _lootFarmWebClient;

    public UpdateMarketItemPricesFromLootFarmtJob(SteamDbContext db, LootFarmWebClient lootFarmWebClient)
    {
        _db = db;
        _lootFarmWebClient = lootFarmWebClient;
    }

    [Function("Update-Market-Item-Prices-From-LootFarm")]
    public async Task Run([TimerTrigger("0 2-59/15 * * * *")] /* every 15mins */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Prices-From-LootFarm");

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
            logger.LogTrace($"Updating market item price information from LOOT.Farm (appId: {app.SteamId})");
            var items = await _db.SteamMarketItems
                .Select(x => new
                {
                    Name = x.Description.NameHash,
                    Currency = x.Currency,
                    Item = x,
                })
                .ToListAsync();

            try
            {
                var lootFarmItems = await _lootFarmWebClient.GetItemPricesAsync(app.Name);
                if (lootFarmItems?.Any() != true)
                {
                    continue;
                }

                foreach (var lootFarmItem in lootFarmItems)
                {
                    var item = items.FirstOrDefault(x => x.Name == lootFarmItem.Name)?.Item;
                    if (item != null)
                    {
                        item.UpdateBuyPrices(MarketType.LOOTFarm, new PriceWithSupply
                        {
                            Price = lootFarmItem.Have > 0 ? item.Currency.CalculateExchange(lootFarmItem.Price, usdCurrency) : 0,
                            Supply = lootFarmItem.Have
                        });
                    }
                }

                var missingItems = items.Where(x => !lootFarmItems.Any(y => x.Name == y.Name) && x.Item.BuyPrices.ContainsKey(MarketType.LOOTFarm));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.UpdateBuyPrices(MarketType.LOOTFarm, null);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item price information from LOOT.Farm (appId: {app.SteamId}). {ex.Message}");
                continue;
            }

            _db.SaveChanges();
        }
    }
}
