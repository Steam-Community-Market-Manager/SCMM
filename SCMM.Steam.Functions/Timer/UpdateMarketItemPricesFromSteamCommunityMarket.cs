using CommandQuery;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Abstractions.Statistics;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Models.Statistics;
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
    private readonly SteamCommunityWebClient _steamCommunityWebClient;
    private readonly IStatisticsService _statisticsService;
    private readonly ICommandProcessor _commandProcessor;

    public UpdateMarketItemPricesSteamCommunityMarket(SteamDbContext db, SteamCommunityWebClient steamCommunityWebClient, IStatisticsService statisticsService, ICommandProcessor commandProcessor)
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
        var usdCurrency = _db.SteamCurrencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyUSD);
        if (usdCurrency == null)
        {
            return;
        }

        foreach (var app in supportedSteamApps)
        {
            logger.LogTrace($"Updating market item price information from steam community market (appId: {app.SteamId})");
            await UpdateSteamCommunityMarketPricesForApp(logger, app, usdCurrency);
        }
    }

    private async Task UpdateSteamCommunityMarketPricesForApp(ILogger logger, SteamApp app, SteamCurrency usdCurrency)
    {
        var statisticsKey = String.Format(StatisticKeys.MarketStatusByAppId, app.SteamId);
        var stopwatch = new Stopwatch();
        try
        {
            stopwatch.Start();

            var paginationStart = 0;
            var paginationCount = SteamMarketSearchPaginatedJsonRequest.MaxPageSize;
            var steamSearchResults = (SteamMarketSearchPaginatedJsonResponse)null;
            var allMarketItems = new List<SteamMarketItem>();
            do
            {
                steamSearchResults = await _steamCommunityWebClient.GetMarketSearchPaginatedAsync(
                    new SteamMarketSearchPaginatedJsonRequest()
                    {
                        AppId = app.SteamId,
                        GetDescriptions = true,
                        SortColumn = SteamMarketSearchPaginatedJsonRequest.SortColumnName,
                        Start = paginationStart,
                        Count = paginationCount
                    },
                    useCache: false
                );

                paginationStart += steamSearchResults.Results?.Count ?? 0;
                if ((steamSearchResults?.Success) != true || !(steamSearchResults?.Results?.Count > 0))
                {
                    continue;
                }

                try
                {
                    var importedAssetDescriptions = await _commandProcessor.ProcessWithResultAsync(new ImportSteamAssetDescriptionsRequest()
                    {
                        AppId = UInt64.Parse(app.SteamId),
                        AssetClassIds = steamSearchResults.Results
                        .Select(x => x?.AssetDescription?.ClassId ?? 0)
                        .Where(x => x > 0)
                        .ToArray(),
                    });

                    var assetDescriptionIds = importedAssetDescriptions.AssetDescriptions.Select(x => x.Id).ToArray();
                    var dbItems = await _db.SteamMarketItems
                        .Where(x => x.DescriptionId != null && assetDescriptionIds.Contains(x.DescriptionId.Value))
                        .ToListAsync();

                    foreach (var assetDescription in importedAssetDescriptions.AssetDescriptions)
                    {
                        var dbItem = (assetDescription.MarketItem ?? dbItems?.FirstOrDefault(x => x.DescriptionId == assetDescription.Id));
                        if (dbItem == null)
                        {
                            dbItem = assetDescription.MarketItem = new SteamMarketItem()
                            {
                                SteamId = assetDescription.NameId?.ToString(),
                                AppId = app.Id,
                                App = app,
                                Description = assetDescription,
                                Currency = usdCurrency,
                            };
                        }

                        var steamItem = steamSearchResults.Results.FirstOrDefault(x => x.AssetDescription?.ClassId == assetDescription.ClassId);
                        if (steamItem?.SellPrice > 0)
                        {
                            dbItem.SellOrderLowestPrice = steamItem.SellPrice;
                        }
                        if (steamItem?.SellListings > 0)
                        {
                            dbItem.SellOrderCount = steamItem.SellListings;
                        }
                        if (dbItem.SellOrderLowestPrice > 0)
                        {
                            dbItem.UpdateBuyPrices(SteamCommunityMarket, new PriceWithSupply()
                            {
                                Price = dbItem.SellOrderLowestPrice,
                                Supply = (dbItem.SellOrderCount > 0 ? dbItem.SellOrderCount : null)
                            });
                        }

                        allMarketItems.Add(dbItem);
                    }

                    await _db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Failed to update batch of market item prices from the steam community market (appId: {app.SteamId}). {ex.Message}");
                }

            } while (steamSearchResults?.Success == true && steamSearchResults?.Results?.Count > 0);

            await _db.SaveChangesAsync();

            await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, SteamCommunityMarket, x =>
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
                await _statisticsService.PatchDictionaryValueAsync<MarketType, MarketStatusStatistic>(statisticsKey, SteamCommunityMarket, x =>
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
    }
}
