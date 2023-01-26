using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Client.Commands;
using SCMM.Shared.Abstractions.Analytics;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Models.Statistics;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.Community.Responses.Json;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;
using System.Linq;

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

        [Command("import-market-items")]
        public async Task<RuntimeResult> ImportMarketItems(ulong appId)
        {
            var message = await Context.Message.ReplyAsync("Importing market items...");

            var app = await _steamDb.SteamApps
                .FirstOrDefaultAsync(x => x.SteamId == appId.ToString());

            var defaultCurrency = await _steamDb.SteamCurrencies
                .FirstOrDefaultAsync(x => x.Name == Constants.SteamDefaultCurrency);

            var paginationStart = 0;
            var paginationCount = SteamMarketSearchPaginatedJsonRequest.MaxPageSize;
            var searchResults = (SteamMarketSearchPaginatedJsonResponse)null;
            do
            {
                await message.ModifyAsync(
                    x => x.Content = $"Importing market items {paginationStart}/{searchResults?.TotalCount.ToString() ?? "???"}..."
                );

                searchResults = await _communityClient.GetMarketSearchPaginated(new SteamMarketSearchPaginatedJsonRequest()
                {
                    AppId = appId.ToString(),
                    GetDescriptions = true,
                    SortColumn = SteamMarketSearchPaginatedJsonRequest.SortColumnName,
                    Start = paginationStart,
                    Count = paginationCount
                }, useCache: false);
                paginationStart += paginationCount;

                if (searchResults?.Success == true && searchResults.Results != null)
                {
                    var importedAssetDescriptions = await _commandProcessor.ProcessWithResultAsync(new ImportSteamAssetDescriptionsRequest()
                    {
                        AppId = appId,
                        AssetClassIds = searchResults.Results
                            .Select(x => x?.AssetDescription?.ClassId ?? 0)
                            .Where(x => x > 0)
                            .ToArray(),
                    });

                    var descriptionIds = importedAssetDescriptions.AssetDescriptions.Select(x => x.Id).ToArray();
                    var marketItems = await _steamDb.SteamMarketItems
                        .Where(x => x.DescriptionId != null && descriptionIds.Contains(x.DescriptionId.Value))
                        .ToListAsync();

                    foreach (var assetDescription in importedAssetDescriptions.AssetDescriptions)
                    {
                        var marketItem = assetDescription?.MarketItem;
                        if (marketItem == null)
                        {
                            marketItem = assetDescription.MarketItem = new SteamMarketItem()
                            {
                                SteamId = assetDescription.NameId?.ToString(),
                                AppId = app.Id,
                                App = app,
                                Description = assetDescription,
                                Currency = defaultCurrency,
                            };
                        }

                        var item = searchResults.Results.FirstOrDefault(x => x.AssetDescription.ClassId == assetDescription.ClassId);
                        if (item?.SellPrice > 0)
                        {
                            marketItem.SellOrderLowestPrice = item.SellPrice;
                        }
                        if (item?.SellListings > 0)
                        {
                            marketItem.SellOrderCount = item.SellListings;
                        }
                        if (marketItem.SellOrderLowestPrice > 0)
                        {
                            marketItem.UpdateBuyPrices(MarketType.SteamCommunityMarket, new PriceWithSupply()
                            {
                                Price = marketItem.SellOrderLowestPrice,
                                Supply = (marketItem.SellOrderCount > 0 ? marketItem.SellOrderCount : null)
                            });
                        }
                    }

                    await _steamDb.SaveChangesAsync();
                }

            } while (searchResults?.Success == true && searchResults?.Results?.Count > 0);

            var itemCount = (searchResults?.TotalCount.ToString() ?? "???");
            await message.ModifyAsync(
                x => x.Content = $"Imported market items {itemCount}/{itemCount}"
            );

            return CommandResult.Success();
        }
    }
}
