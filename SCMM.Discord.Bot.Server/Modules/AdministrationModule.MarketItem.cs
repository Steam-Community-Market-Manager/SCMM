using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Client.Commands;
using SCMM.Shared.Abstractions.Analytics;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Models.Statistics;
using SCMM.Steam.Data.Models;

namespace SCMM.Discord.Bot.Server.Modules
{
    public partial class AdministrationModule
    {
        [Command("rebuild-market-index-fund-stats")]
        public async Task<RuntimeResult> RebuildMarketIndexFundStats(ulong appId)
        {
            var yesterday = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
            var start = _steamDb.SteamMarketItemSale.Min(x => x.Timestamp).Date;
            var end = _steamDb.SteamMarketItemSale.Max(x => x.Timestamp).Date;
            var indexFund = new Dictionary<DateTime, IndexFundStatistic>();

            try
            {
                var message = await Context.Message.ReplyAsync("Rebuilding market index fund...");
                while (start < end)
                {
                    await message.ModifyAsync(
                        x => x.Content = $"Rebuilding market index fund {start.Date.ToString()}..."
                    );
                    indexFund[start] = _steamDb.SteamMarketItemSale
                        .AsNoTracking()
                        .Where(x => x.Item.App.SteamId == appId.ToString())
                        .Where(x => x.Timestamp >= start && x.Timestamp < start.AddDays(1))
                        .ToList()
                        .GroupBy(x => true)
                        .Select(x => new IndexFundStatistic
                        {
                            TotalItems = x.GroupBy(x => x.ItemId).Count(),
                            TotalSalesVolume = x.Sum(y => y.Quantity),
                            TotalSalesValue = x.Sum(y => y.MedianPrice * y.Quantity),
                            AverageItemValue = x.GroupBy(x => x.ItemId).Average(y => y.Average(z => z.MedianPrice))
                        })
                        .FirstOrDefault();

                    start = start.AddDays(1);
                }

                await message.ModifyAsync(
                    x => x.Content = $"Rebuilt market index fund"
                );

                return CommandResult.Success();
            }
            finally
            {
                if (indexFund.Any())
                {
                    await _statisticsService.SetDictionaryAsync(
                        String.Format(StatisticKeys.IndexFundByAppId, appId),
                        indexFund
                            .OrderBy(x => x.Key)
                            .ToDictionary(x => x.Key, x => x.Value)
                    );
                }
            }
        }

        [Command("find-sales-history-anomalies")]
        public async Task<RuntimeResult> FindSalesHistoryAnomaliesAsync([Remainder] string itemName)
        {
            var cutoff = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(30));
            var item = await _steamDb.SteamMarketItems.FirstOrDefaultAsync(x => x.Description.Name == itemName);
            var priceData = await _steamDb.SteamMarketItemSale.Where(x => x.ItemId == item.Id && x.Timestamp >= cutoff).OrderByDescending(x => x.Timestamp).Take(168).ToListAsync();

            var priceAnomalies = await _timeSeriesAnalysisService.DetectTimeSeriesAnomaliesAsync(
                priceData.ToDictionary(x => x.Timestamp, x => (float)x.MedianPrice),
                granularity: TimeGranularity.Hourly,
                sensitivity: 90
            );
            var quantityAnomalies = await _timeSeriesAnalysisService.DetectTimeSeriesAnomaliesAsync(
                priceData.ToDictionary(x => x.Timestamp, x => (float)x.Quantity),
                granularity: TimeGranularity.Hourly,
                sensitivity: 90
            );

            var anomalies = priceAnomalies.Union(quantityAnomalies);
            foreach (var anomaly in priceAnomalies.Where(x => x.IsPositive).OrderBy(x => x.Timestamp))
            {
                var type = (priceAnomalies.Contains(anomaly)) ? "PRICE" : "QUANTITY";
                await Context.Channel.SendMessageAsync($"{type} ANOMALY @ {anomaly.Timestamp} (actual {anomaly.ActualValue}, expected {anomaly.ExpectedValue}, upper: {anomaly.UpperMargin}, lower: {anomaly.LowerMargin}, positive: {anomaly.IsPositive}, negative: {anomaly.IsNegative}, severity: {anomaly.Severity})");
            }

            return CommandResult.Success();
        }
    }
}
