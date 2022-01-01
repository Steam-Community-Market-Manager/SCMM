using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Market.LootFarm.Client;
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
    public async Task Run([TimerTrigger("0 20 * * * *")] /* every hour, 20 minutes after the hour */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Prices-From-LootFarm");

        var steamApps = await _db.SteamApps.ToListAsync();
        if (!steamApps.Any())
        {
            return;
        }

        foreach (var app in steamApps)
        {
            try
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

                var lootFarmItems = await _lootFarmWebClient.GetItemListAsync(app.Name);
                if (lootFarmItems?.Any() != true)
                {
                    continue;
                }

                foreach (var lootFarmItem in lootFarmItems)
                {
                    var item = items.FirstOrDefault(x => x.Name == lootFarmItem.Name)?.Item;
                    if (item != null)
                    {
                        item.Prices = new PersistablePriceStockDictionary(item.Prices);
                        item.Prices[PriceType.LOOTFarm] = new PriceStock
                        {
                            Price = lootFarmItem.Have > 0 ? lootFarmItem.Price : 0,
                            Stock = lootFarmItem.Have
                        };
                    }
                }

                var missingItems = items.Where(x => !lootFarmItems.Any(y => x.Name == y.Name) && x.Item.Prices.ContainsKey(PriceType.LOOTFarm));
                foreach (var missingItem in missingItems)
                {
                    missingItem.Item.Prices = new PersistablePriceStockDictionary(missingItem.Item.Prices);
                    missingItem.Item.Prices.Remove(PriceType.LOOTFarm);
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
