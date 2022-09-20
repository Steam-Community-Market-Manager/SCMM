using CommandQuery;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Azure.ServiceBus;
using SCMM.Shared.API.Events;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.API;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.Functions.Timer;

public class CheckForNewMarketItems
{
    private readonly IConfiguration _configuration;
    private readonly SteamDbContext _steamDb;
    private readonly ICommandProcessor _commandProcessor;
    private readonly IQueryProcessor _queryProcessor;
    private readonly SteamCommunityWebClient _steamCommunityWebClient;
    private readonly SteamService _steamService;
    private readonly ServiceBusClient _serviceBus;

    public CheckForNewMarketItems(IConfiguration configuration, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, SteamDbContext steamDb, SteamCommunityWebClient steamCommunityWebClient, SteamService steamService, ServiceBusClient serviceBus)
    {
        _configuration = configuration;
        _commandProcessor = commandProcessor;
        _queryProcessor = queryProcessor;
        _steamDb = steamDb;
        _steamCommunityWebClient = steamCommunityWebClient;
        _steamService = steamService;
        _serviceBus = serviceBus;
    }

    [Function("Check-New-Market-Items")]
    public async Task Run([TimerTrigger("0 3 * * * *")] /* every hour, 3 minutes after the hour */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Check-New-Market-Items");

        var assetDescriptions = _steamDb.SteamAssetDescriptions
            .Where(x => x.MarketItem == null && (x.IsMarketable || x.MarketableRestrictionDays > 0))
            .Where(x => !String.IsNullOrEmpty(x.NameHash))
            .Where(x => !x.IsSpecialDrop && !x.IsTwitchDrop)
            .Where(x => x.IsAccepted)
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

        // TODO: Check up to 5 times with a 1min delay between each attempt

        logger.LogTrace($"Checking for new market items (assets: {assetDescriptions.Count})");
        var newMarketItems = new List<SteamMarketItem>();
        foreach (var assetDescription in assetDescriptions)
        {
            try
            {
                // TODO: Find a better way to deal with Steam's rate limiting.
                Thread.Sleep(3000);

                var marketPriceOverviewRequest = new SteamMarketPriceOverviewJsonRequest()
                {
                    AppId = assetDescription.App.SteamId,
                    MarketHashName = assetDescription.NameHash,
                    Language = Constants.SteamDefaultLanguage,
                    CurrencyId = usdCurrency.SteamId,
                    NoRender = true
                };

                var marketPriceOverviewResponse = await _steamCommunityWebClient.GetMarketPriceOverview(marketPriceOverviewRequest);
                if (marketPriceOverviewResponse?.Success == true)
                {
                    var newMarketItem = await _steamService.AddOrUpdateMarketItem(assetDescription.App, usdCurrency, marketPriceOverviewResponse, assetDescription);
                    if (newMarketItem != null)
                    {
                        newMarketItems.Add(newMarketItem);
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

        if (newMarketItems.Any())
        {
            logger.LogInformation($"New market items detected!");
            _steamDb.SaveChanges();
        }

        var newMarketItemGroups = newMarketItems.GroupBy(x => x.App).Where(x => x.Key.IsActive);
        foreach (var newMarketItemGroup in newMarketItemGroups)
        {
            await BroadcastMarketItemAddedMessages(logger, newMarketItemGroup.Key, newMarketItemGroup.ToArray());
        }
    }

    private async Task BroadcastMarketItemAddedMessages(ILogger logger, SteamApp app, IEnumerable<SteamMarketItem> newMarketItems)
    {
        newMarketItems = newMarketItems?.OrderBy(x => x.Description.Name)?.ToArray();
        if (newMarketItems?.Any() != true)
        {
            return;
        }

        var broadcastTasks = new List<Task>();
        foreach (var marketItem in newMarketItems)
        {
            broadcastTasks.Add(
                _serviceBus.SendMessageAsync(new MarketItemAddedMessage()
                {
                    AppId = UInt64.Parse(app.SteamId),
                    AppName = app.Name,
                    AppIconUrl = app.IconUrl,
                    AppColour = app.PrimaryColor,
                    CreatorId = marketItem.Description?.CreatorId,
                    CreatorName = marketItem.Description?.CreatorProfile?.Name,
                    CreatorAvatarUrl = marketItem.Description?.CreatorProfile?.AvatarUrl,
                    ItemId = UInt64.Parse(marketItem.SteamId),
                    ItemType = marketItem.Description?.ItemType,
                    ItemShortName = marketItem.Description?.ItemShortName,
                    ItemName = marketItem.Description?.Name,
                    ItemDescription = marketItem.Description?.Description,
                    ItemCollection = marketItem.Description?.ItemCollection,
                    ItemImageUrl = marketItem.Description?.PreviewUrl ?? marketItem.Description?.IconLargeUrl ?? marketItem.Description?.IconUrl,
                })
            );
        }

        await Task.WhenAll(broadcastTasks);
    }
}
