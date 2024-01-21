using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItem24hrSnapshots
{
    private readonly SteamDbContext _db;

    public UpdateMarketItem24hrSnapshots(SteamDbContext db)
    {
        _db = db;
    }

    [Function("Update-Market-Item-24hr-Snapshots")]
    public async Task Run([TimerTrigger("0 0 0 * * *")] /* every day at midnight */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-24hr-Value-Snapshots");

        var marketItems = await _db.SteamMarketItems
            .Where(x => (x.App.FeatureFlags & SteamAppFeatureFlags.ItemMarketPriceTracking) != 0)
            .ToListAsync();

        if (!marketItems.Any())
        {
            return;
        }

        foreach (var marketItem in marketItems)
        {
            // Update stable 24hr value snapshots
            marketItem.Stable24hrBuyOrderHighestPrice = marketItem.BuyOrderHighestPrice;
            marketItem.Stable24hrSellOrderLowestPrice = marketItem.SellOrderLowestPrice;
            marketItem.Stable24hrValue = marketItem.LastSaleValue;

            // Update rolling 24hr value snapshots
            marketItem.BuyOrderHighestPriceRolling24hrs = new PersistablePriceCollection(
                marketItem.BuyOrderHighestPriceRolling24hrs.Prepend(marketItem.Stable24hrBuyOrderHighestPrice).Take(24)
            );
            marketItem.SellOrderLowestPriceRolling24hrs = new PersistablePriceCollection(
                marketItem.SellOrderLowestPriceRolling24hrs.Prepend(marketItem.Stable24hrSellOrderLowestPrice).Take(24)
            );
            marketItem.SalesPriceRolling24hrs = new PersistablePriceCollection(
                marketItem.SalesPriceRolling24hrs.Prepend(marketItem.Stable24hrValue).Take(24)
            );
        }

        await _db.SaveChangesAsync();
    }
}
