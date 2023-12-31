﻿using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.Functions.Timer;

public class UpdateProfileInventoryValues
{
    private readonly SteamDbContext _db;

    public UpdateProfileInventoryValues(SteamDbContext db)
    {
        _db = db;
    }

    [Function("Update-Profile-Inventory-Values")]
    public async Task Run([TimerTrigger("0 45 * * * *")] /* every hour, 45 minutes past the hour */ TimerInfo timerInfo, FunctionContext context)
    {
        using (var transaction = await _db.Database.BeginTransactionAsync())
        {
            _db.Database.SetCommandTimeout(900); // 15mins max

            await _db.Database.ExecuteSqlInterpolatedAsync(@$"
                UPDATE v 
                SET [MarketValue] = ISNULL((
                    SELECT SUM(i.Quantity * ISNULL(m.SellOrderLowestPrice, ISNULL(m.BuyNowPrice, 0)))
                    FROM [SteamProfileInventoryItems] i
                    LEFT OUTER JOIN [SteamMarketItems] m ON m.DescriptionId = i.DescriptionId
                    WHERE i.ProfileId = v.ProfileId AND i.AppId = v.AppId
                ), 0)
                FROM [SteamProfileInventoryValues] v
	            WHERE v.Items > 0
            ");

            await transaction.CommitAsync();
        }
    }
}
