using CommandQuery;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.API.Events;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.Community.Responses.Json;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.Functions.Timer;

public class CheckForNewMarketItems
{
    private readonly IConfiguration _configuration;
    private readonly SteamDbContext _steamDb;
    private readonly ICommandProcessor _commandProcessor;
    private readonly IQueryProcessor _queryProcessor;
    private readonly SteamCommunityWebClient _steamCommunityWebClient;
    private readonly IServiceBus _serviceBus;

    public CheckForNewMarketItems(IConfiguration configuration, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, SteamDbContext steamDb, SteamCommunityWebClient steamCommunityWebClient, IServiceBus serviceBus)
    {
        _configuration = configuration;
        _commandProcessor = commandProcessor;
        _queryProcessor = queryProcessor;
        _steamDb = steamDb;
        _steamCommunityWebClient = steamCommunityWebClient;
        _serviceBus = serviceBus;
    }

    [Function("Check-New-Market-Items")]
    public async Task Run([TimerTrigger("0 0-15/2 * * * *")] /* every 2 minutes, minutes 0 through 15 past the hour */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Check-New-Market-Items");

        var assetDescriptions = _steamDb.SteamAssetDescriptions
            .Where(x => x.MarketItem == null && (x.IsMarketable || x.MarketableRestrictionDays > 0))
            .Where(x => !String.IsNullOrEmpty(x.NameHash))
            .Where(x => !x.IsPublisherDrop && !x.IsTwitchDrop)
            .Where(x => x.IsAccepted)
            .Where(x => x.App.IsActive)
            .Include(x => x.App)
            .Include(x => x.CreatorProfile)
            .ToList();
        if (!assetDescriptions.Any())
        {
            return;
        }

        var usdCurrency = _steamDb.SteamCurrencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyUSD);
        if (usdCurrency == null)
        {
            return;
        }

        logger.LogTrace($"Checking for new market items (assets: {assetDescriptions.Count})");
        var newMarketItems = new List<SteamMarketItem>();
        foreach (var assetDescription in assetDescriptions.OrderBy(x => x.Name))
        {
            try
            {
                var app = assetDescription.App;
                var marketPriceOverviewRequest = new SteamMarketPriceOverviewJsonRequest()
                {
                    AppId = app.SteamId,
                    MarketHashName = assetDescription.NameHash,
                    Language = Constants.SteamDefaultLanguage,
                    CurrencyId = usdCurrency.SteamId,
                    NoRender = true
                };

                var marketPriceOverviewResponse = await _steamCommunityWebClient.GetMarketPriceOverview(marketPriceOverviewRequest);
                if (marketPriceOverviewResponse?.Success == true)
                {
                    var newMarketItem = await AddOrUpdateMarketItem(app, usdCurrency, marketPriceOverviewResponse, assetDescription);
                    if (newMarketItem != null)
                    {
                        logger.LogTrace($"New market item found (appId: {app.SteamId}, classId: {assetDescription.ClassId}, name: '{assetDescription.Name}')");
                        await _serviceBus.SendMessageAsync(new MarketItemAddedMessage()
                        {
                            AppId = UInt64.Parse(app.SteamId),
                            AppName = app.Name,
                            AppIconUrl = app.IconUrl,
                            AppColour = app.PrimaryColor,
                            CreatorId = newMarketItem.Description?.CreatorId,
                            CreatorName = newMarketItem.Description?.CreatorProfile?.Name,
                            CreatorAvatarUrl = newMarketItem.Description?.CreatorProfile?.AvatarUrl,
                            ItemId = UInt64.Parse(newMarketItem.SteamId),
                            ItemType = newMarketItem.Description?.ItemType,
                            ItemShortName = newMarketItem.Description?.ItemShortName,
                            ItemName = newMarketItem.Description?.Name,
                            ItemDescription = newMarketItem.Description?.Description,
                            ItemCollection = newMarketItem.Description?.ItemCollection,
                            ItemIconUrl = newMarketItem.Description?.IconUrl ?? newMarketItem.Description?.IconLargeUrl,
                            ItemImageUrl = newMarketItem.Description?.PreviewUrl ?? newMarketItem.Description?.IconLargeUrl ?? newMarketItem.Description?.IconUrl,
                        });
                    }
                }
            }
            catch (SteamRequestException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    // This means the item doesn't have a price summary (isn't yet marketable).
                    // We can sliently ignore this error...
                }
                else
                {
                    logger.LogError(ex, $"Failed to check new market item (classId: {assetDescription.ClassId}). {ex.Message}");
                }
            }
        }

        _steamDb.SaveChanges();
    }

    private async Task<SteamMarketItem> AddOrUpdateMarketItem(SteamApp app, SteamCurrency currency, SteamMarketPriceOverviewJsonResponse marketPriceOverview, SteamAssetDescription asset)
    {
        var dbItem = await _steamDb.SteamMarketItems
            .Include(x => x.App)
            .Include(x => x.Currency)
            .Include(x => x.Description)
            .Where(x => x.AppId == app.Id)
            .FirstOrDefaultAsync(x => x.Description.ClassId == asset.ClassId);

        if (dbItem != null)
        {
            return dbItem;
        }

        if (asset.ClassId == null)
        {
            return null;
        }

        var importAssetDescription = await _commandProcessor.ProcessWithResultAsync(new ImportSteamAssetDescriptionRequest()
        {
            AppId = ulong.Parse(app.SteamId),
            AssetClassId = asset.ClassId.Value
        });
        var assetDescription = importAssetDescription.AssetDescription;
        if (assetDescription == null || assetDescription.NameId == null)
        {
            return null;
        }

        app.MarketItems.Add(dbItem = new SteamMarketItem()
        {
            SteamId = assetDescription.NameId?.ToString(),
            AppId = app.Id,
            Description = assetDescription,
            Currency = currency,
            SellOrderCount = marketPriceOverview.Volume.SteamQuantityValueAsInt(),
            SellOrderLowestPrice = marketPriceOverview.LowestPrice.SteamPriceAsInt()
        });

        return dbItem;
    }
}
