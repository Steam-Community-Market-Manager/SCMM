using CommandQuery;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Abstractions.Statistics;
using SCMM.Shared.Data.Models.Statistics;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.Community.Responses.Json;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;
using System.Diagnostics;
namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemPricesSteamCommunityMarket
{
    private const MarketType SteamCommunityMarket = MarketType.SteamCommunityMarket;

    private readonly SteamDbContext _db;
    private readonly ProxiedSteamCommunityWebClient _steamCommunityWebClient;
    private readonly IStatisticsService _statisticsService;
    private readonly ICommandProcessor _commandProcessor;

    public UpdateMarketItemPricesSteamCommunityMarket(SteamDbContext db, ProxiedSteamCommunityWebClient steamCommunityWebClient, IStatisticsService statisticsService, ICommandProcessor commandProcessor)
    {
        _db = db;
        _steamCommunityWebClient = steamCommunityWebClient;
        _statisticsService = statisticsService;
        _commandProcessor = commandProcessor;
    }

    //[Function("Update-Market-Item-Prices-From-Steam-Community-Market")]
    public async Task Run([TimerTrigger("0 0 0/12 * * *")] /* every 12 hours */ TimerInfo timerInfo, FunctionContext context)
    {
        if (!SteamCommunityMarket.IsEnabled())
        {
            return;
        }

        var logger = context.GetLogger("Update-Market-Item-Prices-From-Steam-Community-Market");
        var stopwatch = new Stopwatch();

        var appIds = SteamCommunityMarket.GetSupportedAppIds().Select(x => x.ToString()).ToArray();
        var supportedSteamApps = await _db.SteamApps
            .Where(x => appIds.Contains(x.SteamId))
            //.Where(x => x.IsActive)
            .ToListAsync();
        if (!supportedSteamApps.Any())
        {
            return;
        }

        // Prices are returned in USD by default
        var defaultCurrency = _db.SteamCurrencies.FirstOrDefault(x => x.Name == Constants.SteamDefaultCurrency);
        if (defaultCurrency == null)
        {
            return;
        }

        foreach (var app in supportedSteamApps)
        {
            logger.LogTrace($"Updating market item price information from steam community market (appId: {app.SteamId})");
            var statisticsKey = String.Format(StatisticKeys.MarketStatusByAppId, app.SteamId);

            try
            {
                stopwatch.Restart();

                var paginationStart = 0;
                var paginationCount = SteamMarketSearchPaginatedJsonRequest.MaxPageSize;
                var searchResults = (SteamMarketSearchPaginatedJsonResponse)null;
                var allMarketItems = new List<SteamMarketItem>();
                do
                {
                    searchResults = await _steamCommunityWebClient.GetMarketSearchPaginated(new SteamMarketSearchPaginatedJsonRequest()
                    {
                        AppId = app.SteamId,
                        GetDescriptions = true,
                        SortColumn = SteamMarketSearchPaginatedJsonRequest.SortColumnName,
                        Start = paginationStart,
                        Count = paginationCount
                    }, useCache: false);
                    paginationStart += searchResults.Results?.Count ?? 0;

                    if (searchResults?.Success == true && searchResults?.Results?.Count > 0)
                    {
                        var importedAssetDescriptions = await _commandProcessor.ProcessWithResultAsync(new ImportSteamAssetDescriptionsRequest()
                        {
                            AppId = UInt64.Parse(app.SteamId),
                            AssetClassIds = searchResults.Results
                                .Select(x => x?.AssetDescription?.ClassId ?? 0)
                                .Where(x => x > 0)
                                .ToArray(),
                        });

                        var descriptionIds = importedAssetDescriptions.AssetDescriptions.Select(x => x.Id).ToArray();
                        var marketItems = await _db.SteamMarketItems
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
                                marketItem.UpdateBuyPrices(SteamCommunityMarket, new PriceWithSupply()
                                {
                                    Price = marketItem.SellOrderLowestPrice,
                                    Supply = (marketItem.SellOrderCount > 0 ? marketItem.SellOrderCount : null)
                                });
                            }

                            allMarketItems.Add(marketItem);
                        }

                        await _db.SaveChangesAsync();
                    }

                } while (searchResults?.Success == true && searchResults?.Results?.Count > 0);

                await _db.SaveChangesAsync();

                await _statisticsService.UpdateDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, SteamCommunityMarket, x =>
                {
                    x.TotalItems = allMarketItems.Count();
                    x.TotalListings = allMarketItems.Sum(i => i.SellOrderCount);
                    x.LastUpdatedItemsOn = DateTimeOffset.Now;
                    x.LastUpdatedItemsDuration = stopwatch.Elapsed;
                    x.LastUpdateErrorOn = null;
                    x.LastUpdateError = null;
                });
            }
            catch (Exception ex)
            {
                try
                {
                    logger.LogError(ex, $"Failed to update trade item price information from steam community market (appId: {app.SteamId}). {ex.Message}");
                    await _statisticsService.UpdateDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, SteamCommunityMarket, x =>
                    {
                        x.LastUpdateErrorOn = DateTimeOffset.Now;
                        x.LastUpdateError = ex.Message;
                    });
                }
                catch (Exception)
                {
                    logger.LogError(ex, $"Failed to update trade item price statistics for steam community market (appId: {app.SteamId}). {ex.Message}");
                }
            }
            finally
            {
                stopwatch.Stop();
            }
        }
    }
}
