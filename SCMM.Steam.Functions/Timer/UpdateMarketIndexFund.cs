using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Abstractions.Statistics;
using SCMM.Shared.Data.Models.Statistics;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketIndexFund
{
    private readonly SteamDbContext _db;
    private readonly IStatisticsService _statisticsService;

    public UpdateMarketIndexFund(SteamDbContext db, IStatisticsService statisticsService)
    {
        _db = db;
        _statisticsService = statisticsService;
    }

    [Function("Update-Market-Index-Fund")]
    public async Task Run([TimerTrigger("0 30 0 * * *")] /* every day, 30 minutes after midnight */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Index-Fund");

        var apps = await _db.SteamApps
            .Where(x => x.IsActive)
            .ToArrayAsync();

        foreach (var app in apps)
        {
            var indexFund = new Dictionary<DateTime, IndexFundStatistic>();
            var end = _db.SteamMarketItemSale.Max(x => x.Timestamp).Date;
            var start = end.AddDays(-3);

            // TODO: Read the current index fund dictionary from stats service, only process missing dates

            try
            {
                while (start < end)
                {
                    var stats = _db.SteamMarketItemSale
                        .AsNoTracking()
                        .Where(x => x.Item.AppId == app.Id)
                        .Where(x => x.Timestamp >= start && x.Timestamp < start.AddDays(1))
                        .GroupBy(x => x.ItemId)
                        .Select(x => new
                        {
                            TotalSalesVolume = x.Sum(y => y.Quantity),
                            TotalSalesValue = x.Sum(y => y.MedianPrice * y.Quantity),
                            AverageItemValue = x.Average(y => y.MedianPrice)
                        })
                        .ToList()
                        .GroupBy(x => true)
                        .Select(x => new IndexFundStatistic
                        {
                            TotalItems = x.Count(),
                            TotalSalesVolume = x.Sum(y => y.TotalSalesVolume),
                            TotalSalesValue = x.Sum(y => y.TotalSalesValue),
                            AverageItemValue = x.Average(y => y.AverageItemValue)
                        })
                        .FirstOrDefault();

                    if (stats != null)
                    {
                        indexFund[start] = stats;
                    }

                    start = start.AddDays(1);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market index fund for '{app.SteamId}'. {ex.Message}");
            }
            finally
            {
                if (indexFund.Any())
                {
                    await _statisticsService.SetDictionaryAsync(
                        String.Format(StatisticKeys.IndexFundByAppId, app.SteamId),
                        indexFund
                            .OrderBy(x => x.Key)
                            .ToDictionary(x => x.Key, x => x.Value)
                    );
                }
            }
        }
    }
}
