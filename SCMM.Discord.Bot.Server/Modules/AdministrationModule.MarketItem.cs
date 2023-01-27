using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Client.Commands;
using SCMM.Shared.Abstractions.Analytics;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Models.Statistics;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.Community.Responses.Json;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Client.Exceptions;
using System.Globalization;

namespace SCMM.Discord.Bot.Server.Modules
{
    public partial class AdministrationModule
    {
        [Command("rebuild-market-index-fund-stats")]
        public async Task<RuntimeResult> RebuildMarketIndexFundStats(ulong appId)
        {
            var indexFund = new Dictionary<DateTime, IndexFundStatistic>();
            var dates = await _steamDb.SteamMarketItemSale
                .Where(x => x.Item.App.SteamId == appId.ToString())
                .Select(x => x.Timestamp)
                .Distinct()
                .ToListAsync();

            try
            {
                var message = await Context.Message.ReplyAsync("Rebuilding market index fund...");
                foreach (var date in dates.OrderBy(x => x.Date))
                {
                    await message.ModifyAsync(
                        x => x.Content = $"Rebuilding market index fund {date.Date.ToString()}..."
                    );
                    var start = date.Date;
                    var end = date.Date.AddDays(1);
                    var stats = _steamDb.SteamMarketItemSale
                        .AsNoTracking()
                        .Where(x => x.Item.App.SteamId == appId.ToString())
                        .Where(x => x.Timestamp >= start && x.Timestamp < end)
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
                        indexFund[date.Date] = stats;
                    }
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

        [Command("import-market-items-price-history")]
        public async Task<RuntimeResult> ImportMarketItemsPriceHostyr(ulong appId)
        {
            var message = await Context.Message.ReplyAsync("Importing market items price history...");

            var app = await _steamDb.SteamApps
                .FirstOrDefaultAsync(x => x.SteamId == appId.ToString());

            var cutoff = DateTimeOffset.Now.Subtract(TimeSpan.FromHours(1));
            var items = _steamDb.SteamMarketItems
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => x.LastCheckedSalesOn == null)
                //.Where(x => x.LastCheckedSalesOn == null || x.LastCheckedSalesOn <= cutoff)
                //.Where(x => x.App.IsActive)
                .OrderBy(x => x.LastCheckedSalesOn)
                .ToArray();

            if (!items.Any())
            {
                return CommandResult.Success(); ;
            }

            var nzdCurrency = _steamDb.SteamCurrencies.FirstOrDefault(x => x.Name == "NZD");
            if (nzdCurrency == null)
            {
                return CommandResult.Success(); ;
            }

            int unsavedBufferCount = 0;
            foreach (var item in items)
            {
                try
                {
                    await message.ModifyAsync(
                        x => x.Content = $"Importing market items price history {Array.IndexOf(items, item)}/{items.Length}..."
                    );

                    var response = await _authenticatedCommunityClient.GetMarketPriceHistory(
                        new SteamMarketPriceHistoryJsonRequest()
                        {
                            AppId = item.App.SteamId,
                            MarketHashName = item.Description.Name,
                            //CurrencyId = item.Currency.SteamId
                        }
                    );

                    // HACK: Our Steam account is locked to NZD, we must convert all prices to the items currency
                    // TODO: Find/buy a Steam account that is locked to USD for better accuracy
                    await UpdateMarketItemSalesHistory(item, response, nzdCurrency);
                }
                catch (SteamRequestException ex)
                {
                    if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        continue;
                    }
                }
                finally
                {
                    if (unsavedBufferCount >= 100)
                    {
                        await _steamDb.SaveChangesAsync();
                        unsavedBufferCount = 0;
                    }
                    else
                    {
                        unsavedBufferCount++;
                    }
                }
            }

            await message.ModifyAsync(
                x => x.Content = $"Imported market items price history {items.Length}/{items.Length}"
            );

            return CommandResult.Success();
        }

        private async Task<SteamMarketItem> UpdateMarketItemSalesHistory(SteamMarketItem item, SteamMarketPriceHistoryJsonResponse sales, SteamCurrency salesCurrency = null)
        {
            if (item == null || sales?.Success != true)
            {
                return item;
            }

            // Lazy-load sales history if missing, required for recalculation
            if (item.SalesHistory?.Any() != true)
            {
                item = await _steamDb.SteamMarketItems
                    .Include(x => x.SalesHistory)
                    .AsSplitQuery()
                    .SingleOrDefaultAsync(x => x.Id == item.Id);
            }

            // If the sales are not already in our items currency, exchange them now
            var itemSales = ParseMarketItemSalesFromGraph(sales.Prices, item.LastCheckedSalesOn);
            if (itemSales != null && salesCurrency != null && salesCurrency.Id != item.CurrencyId)
            {
                foreach (var sale in itemSales)
                {
                    sale.MedianPrice = item.Currency.CalculateExchange(sale.MedianPrice, salesCurrency);
                }
            }

            item.LastCheckedSalesOn = DateTimeOffset.Now;
            item.RecalculateSales(itemSales);

            return item;
        }

        private SteamMarketItemSale[] ParseMarketItemSalesFromGraph(string[][] salesGraph, DateTimeOffset? ignoreSalesBefore = null)
        {
            var sales = new List<SteamMarketItemSale>();
            if (salesGraph == null)
            {
                return sales.ToArray();
            }

            var totalQuantity = 0;
            for (var i = 0; i < salesGraph.Length; i++)
            {
                var timeStamp = DateTime.ParseExact(salesGraph[i][0], "MMM dd yyyy HH: z", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                var medianPrice = salesGraph[i][1].SteamPriceAsInt();
                var quantity = salesGraph[i][2].SteamQuantityValueAsInt();
                sales.Add(new SteamMarketItemSale()
                {
                    Timestamp = timeStamp,
                    MedianPrice = medianPrice,
                    Quantity = quantity,
                });
                totalQuantity += quantity;
            }

            return sales.ToArray();
        }
    }
}
