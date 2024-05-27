using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.Functions.Timer;

public class UpdateAssetDescriptionSupplyTotals
{
    private readonly SteamDbContext _db;

    public UpdateAssetDescriptionSupplyTotals(SteamDbContext db)
    {
        _db = db;
    }

    [Function("Update-Asset-Description-Supply-Totals")]
    public async Task Run([TimerTrigger("0 20 * * * *")] /* every hour, 20 minutes past the hour */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Asset-Description-Supply-Totals");

        try
        {
            using (var transaction = await _db.Database.BeginTransactionAsync())
            {
                _db.Database.SetCommandTimeout(900); // 15mins max

                // Recalculate the various supply totals
                await _db.Database.ExecuteSqlInterpolatedAsync(@$"
                    ; WITH AssetDescriptionSupply ([Id], SubscriptionsLifetime)
                    AS (
	                    SELECT
		                    a.Id,
		                    a.SubscriptionsLifetime
	                    FROM [SteamAssetDescriptions] a
                    ),
                    MarketSupply ([Id], BuyPricesTotalSupply)
                    AS (
	                    SELECT 
		                    m.DescriptionId, 
		                    m.BuyPricesTotalSupply
	                    FROM [SteamMarketItems] m
                    ),
                    InventorySupply ([Id], UniqueProfileIds, TotalQuantity)
                    AS (
	                    SELECT
		                    i.DescriptionId,
		                    COUNT(DISTINCT(i.ProfileId)),
		                    SUM(i.Quantity)
	                    FROM [SteamProfileInventoryItems] i
                        INNER JOIN [SteamProfiles] p on p.Id = i.ProfileId
                        WHERE p.ItemAnalyticsParticipation >= 0
	                    GROUP BY i.DescriptionId
                    )
                    UPDATE a
                    SET
	                    [SupplyTotalOwnersEstimated] =  IIF([SupplyTotalOwnersEstimated] > ads.SubscriptionsLifetime, [SupplyTotalOwnersEstimated], ads.SubscriptionsLifetime),
	                    [SupplyTotalOwnersKnown] = IIF([SupplyTotalOwnersKnown] > ivs.UniqueProfileIds, [SupplyTotalOwnersKnown], ivs.UniqueProfileIds),
	                    [SupplyTotalInvestorsKnown] = (ivs.TotalQuantity - ivs.UniqueProfileIds),
	                    [SupplyTotalMarketsKnown] = mks.BuyPricesTotalSupply
                    FROM [SteamAssetDescriptions] a
	                    INNER JOIN [SteamApps] app ON app.Id = a.AppId
	                    LEFT OUTER JOIN AssetDescriptionSupply ads ON ads.Id = a.Id
	                    LEFT OUTER JOIN MarketSupply mks ON mks.Id = a.Id
	                    LEFT OUTER JOIN InventorySupply ivs ON ivs.Id = a.Id
                ");

                await transaction.CommitAsync();
            }
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Failed to update asset description supply totals.");
        }

        try
        {
            using (var transaction = await _db.Database.BeginTransactionAsync())
            {
                // Recalculate the overall supply total
                await _db.Database.ExecuteSqlInterpolatedAsync(@$"
                    UPDATE a
                    SET
	                    [SupplyTotalEstimated] = (
		                    (ISNULL(a.SupplyTotalOwnersKnown, 0) + IIF(a.SupplyTotalOwnersKnown > a.SupplyTotalOwnersEstimated, 0, ISNULL((a.SupplyTotalOwnersEstimated - a.SupplyTotalOwnersKnown), 0))) +
		                    (ISNULL(a.SupplyTotalInvestorsKnown, 0) + IIF(a.SupplyTotalInvestorsKnown > a.SupplyTotalInvestorsEstimated, 0, ISNULL((a.SupplyTotalInvestorsEstimated - a.SupplyTotalInvestorsKnown), 0))) +
		                    ISNULL(a.SupplyTotalMarketsKnown, 0)
	                    )
                    FROM [SteamAssetDescriptions] a
                ");

                await transaction.CommitAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update asset description supply total estimations.");
        }
    }
}
