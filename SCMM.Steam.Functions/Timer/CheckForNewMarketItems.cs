using CommandQuery;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Discord.API.Commands;
using SCMM.Discord.Data.Store;
using SCMM.Shared.API.Extensions;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.API;
using SCMM.Steam.API.Commands;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Store;
using System.Globalization;

namespace SCMM.Steam.Functions.Timer;

public class CheckForNewMarketItems
{
    private readonly IConfiguration _configuration;
    private readonly DiscordDbContext _discordDb;
    private readonly SteamDbContext _steamDb;
    private readonly ICommandProcessor _commandProcessor;
    private readonly IQueryProcessor _queryProcessor;
    private readonly SteamCommunityWebClient _steamCommunityWebClient;
    private readonly SteamService _steamService;

    public CheckForNewMarketItems(IConfiguration configuration, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, DiscordDbContext discordDb, SteamDbContext steamDb, SteamCommunityWebClient steamCommunityWebClient, SteamService steamService)
    {
        _configuration = configuration;
        _commandProcessor = commandProcessor;
        _queryProcessor = queryProcessor;
        _discordDb = discordDb;
        _steamDb = steamDb;
        _steamCommunityWebClient = steamCommunityWebClient;
        _steamService = steamService;
    }

    [Function("Check-New-Market-Items")]
    public async Task Run([TimerTrigger("0 2 * * * *")] /* every hour, 2 minutes after the hour */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Check-New-Market-Items");

        var assetDescriptions = _steamDb.SteamAssetDescriptions
            .Where(x => x.MarketItem == null && (x.IsMarketable || x.MarketableRestrictionDays > 0))
            .Where(x => !String.IsNullOrEmpty(x.NameHash))
            .Where(x => !x.IsSpecialDrop && !x.IsTwitchDrop)
            .Where(x => x.IsAccepted)
            .Include(x => x.App)
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
            await BroadcastNewMarketItemsNotification(logger, newMarketItemGroup.Key, newMarketItemGroup.ToArray());
        }
    }

    private async Task BroadcastNewMarketItemsNotification(ILogger logger, SteamApp app, IEnumerable<SteamMarketItem> newMarketItems)
    {
        newMarketItems = newMarketItems?.OrderBy(x => x.Description.Name)?.ToArray();
        if (newMarketItems?.Any() != true)
        {
            return;
        }

        var thumbnailImageUrl = (string)null;
        try
        {
            var itemImageSources = newMarketItems
                .Where(x => x.Description != null)
                .Select(x => new ImageSource()
                {
                    ImageUrl = x.Description.IconUrl,
                    ImageData = x.Description.Icon?.Data,
                })
                .ToList();

            var thumbnailImage = await _queryProcessor.ProcessAsync(new GetImageMosaicRequest()
            {
                ImageSources = itemImageSources,
                ImageSize = 128,
                ImageColumns = 3
            });

            if (thumbnailImage != null)
            {
                thumbnailImageUrl = (
                    await _commandProcessor.ProcessWithResultAsync(new UploadImageToBlobStorageRequest()
                    {
                        Name = $"{app.SteamId}-new-market-items-thumbnail-{DateTime.UtcNow.Ticks}",
                        MimeType = thumbnailImage.MimeType,
                        Data = thumbnailImage.Data,
                        ExpiresOn = DateTimeOffset.Now.AddDays(90)
                    })
                )?.ImageUrl;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate market item thumbnail image");
        }

        var guilds = _discordDb.DiscordGuilds.ToList();
        foreach (var guild in guilds)
        {
            try
            {
                if (!bool.Parse(guild.Get(DiscordGuild.GuildConfiguration.AlertsMarket, Boolean.FalseString).Value))
                {
                    continue;
                }

                var guildChannels = guild.List(DiscordGuild.GuildConfiguration.AlertChannel).Value?.Union(new[] {
                    "announcement", "market", "skin", app.Name, "general", "chat", "bot"
                });

                var fields = new Dictionary<string, string>();
                foreach (var marketItem in newMarketItems)
                {
                    var description = marketItem.Description?.ItemType;
                    if (string.IsNullOrEmpty(description))
                    {
                        description = marketItem.Description?.Description ?? marketItem.SteamId;
                    }
                    if (marketItem.Description?.SupplyTotalEstimated > 0)
                    {
                        description = $"{marketItem.Description?.SupplyTotalEstimated?.ToQuantityString()}+ estimated sales";
                    }
                    fields.Add(marketItem.Description.Name, description);
                }

                var itemImageIds = newMarketItems
                    .Where(x => x.Description?.IconId != null)
                    .Select(x => x.Description.IconId);

                await _commandProcessor.ProcessAsync(new SendDiscordMessageRequest()
                {
                    GuidId = guild.Id,
                    ChannelPatterns = guildChannels?.ToArray(),
                    Message = null,
                    Title = $"{app?.Name} Market - New Listings",
                    Description = $"{newMarketItems.Count()} new item(s) have just appeared in the {app?.Name} marketplace.",
                    Fields = fields,
                    FieldsInline = true,
                    Url = $"{_configuration.GetWebsiteUrl()}/items",
                    ThumbnailUrl = app?.IconUrl,
                    ImageUrl = thumbnailImageUrl,
                    Colour = UInt32.Parse(app.PrimaryColor.Replace("#", ""), NumberStyles.HexNumber)
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to send new market item notification to guild (id: {guild.Id})");
                continue;
            }
        }
    }
}
