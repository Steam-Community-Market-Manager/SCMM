using Azure.AI.AnomalyDetector.Models;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Client.Commands;
using SCMM.Shared.Data.Models.Extensions;

namespace SCMM.Discord.Bot.Server.Modules
{
    public partial class AdministrationModule
    {
        [Command("find-sales-history-anomalies")]
        public async Task<RuntimeResult> FindSalesHistoryAnomaliesAsync([Remainder] string itemName)
        {
            var cutoff = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(30));
            var item = await _steamDb.SteamMarketItems.FirstOrDefaultAsync(x => x.Description.Name == itemName);
            var priceData = await _steamDb.SteamMarketItemSale.Where(x => x.ItemId == item.Id && x.Timestamp >= cutoff).OrderByDescending(x => x.Timestamp).Take(168).ToListAsync();

            var priceAnomalies = await _azureAiClient.DetectTimeSeriesAnomaliesAsync(
                priceData.Select(x => new TimeSeriesPoint(x.MedianPrice)
                {
                    Timestamp = x.Timestamp,
                }),
                granularity: TimeGranularity.Hourly,
                sensitivity: 90
            );
            var quantityAnomalies = await _azureAiClient.DetectTimeSeriesAnomaliesAsync(
                priceData.Select(x => new TimeSeriesPoint(x.Quantity)
                {
                    Timestamp = x.Timestamp,
                }),
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
