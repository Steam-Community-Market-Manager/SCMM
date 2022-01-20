using CommandQuery;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Discord.API.Commands;
using SCMM.Shared.API.Extensions;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store;
using SCMM.Steam.API;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Store;
using System.Globalization;

namespace SCMM.Steam.Functions.Timer;

public class CheckForNewMarketItemsJob
{
    private readonly IConfiguration _configuration;
    private readonly SteamDbContext _db;
    private readonly ICommandProcessor _commandProcessor;
    private readonly IQueryProcessor _queryProcessor;
    private readonly SteamCommunityWebClient _steamCommunityWebClient;
    private readonly SteamService _steamService;

    public CheckForNewMarketItemsJob(IConfiguration configuration, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, SteamDbContext db, SteamCommunityWebClient steamCommunityWebClient, SteamService steamService)
    {
        _configuration = configuration;
        _commandProcessor = commandProcessor;
        _queryProcessor = queryProcessor;
        _db = db;
        _steamCommunityWebClient = steamCommunityWebClient;
        _steamService = steamService;
    }

    [Function("Check-New-Market-Items")]
    public async Task Run([TimerTrigger("0 2 * * * *")] /* every hour, 2 minutes after the hour */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Check-New-Market-Items");

        var assetDescriptions = _db.SteamAssetDescriptions
            .Where(x => x.MarketItem == null && (x.IsMarketable || x.MarketableRestrictionDays > 0))
            .Where(x => !x.IsSpecialDrop && !x.IsTwitchDrop)
            .Where(x => x.TimeAccepted != null)
            .Include(x => x.App)
            .ToList();
        if (!assetDescriptions.Any())
        {
            return;
        }

        var usdCurrency = _db.SteamCurrencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyUSD);
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

            var thumbnailExpiry = DateTimeOffset.Now.AddDays(90);
            var thumbnail = await GenerateMarketItemsThumbnailImage(logger, newMarketItems, thumbnailExpiry);
            if (thumbnail != null)
            {
                _db.FileData.Add(thumbnail);
            }

            _db.SaveChanges();

            await BroadcastNewMarketItemsNotification(logger, newMarketItems, thumbnail);
        }
    }

    private async Task<FileData> GenerateMarketItemsThumbnailImage(ILogger logger, IEnumerable<SteamMarketItem> marketItems, DateTimeOffset expiresOn)
    {
        try
        {
            var items = marketItems.OrderBy(x => x.Description?.Name);
            var itemImageSources = items
                .Where(x => x.Description != null)
                .Select(x => new ImageSource()
                {
                    ImageUrl = x.Description.IconUrl,
                    ImageData = x.Description.Icon?.Data,
                })
                .ToList();

            var thumbnail = await _queryProcessor.ProcessAsync(new GetImageMosaicRequest()
            {
                ImageSources = itemImageSources,
                ImageSize = 128,
                ImageColumns = 3
            });
            if (thumbnail == null)
            {
                return null;
            }

            return new FileData()
            {
                MimeType = thumbnail.MimeType,
                Data = thumbnail.Data,
                ExpiresOn = expiresOn
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate market item thumbnail image");
            return null;
        }
    }

    private async Task BroadcastNewMarketItemsNotification(ILogger logger, IEnumerable<SteamMarketItem> newMarketItems, FileData thumbnailImage)
    {
        newMarketItems = newMarketItems?.OrderBy(x => x.Description.Name);
        var app = newMarketItems.Where(x => x.App != null).FirstOrDefault()?.App;
        var guilds = _db.DiscordGuilds.Include(x => x.Configurations).ToList();
        foreach (var guild in guilds)
        {
            try
            {
                if (!bool.Parse(guild.Get(DiscordConfiguration.AlertsMarket, Boolean.FalseString).Value))
                {
                    continue;
                }

                var guildChannels = guild.List(DiscordConfiguration.AlertChannel).Value?.Union(new[] {
                    "announcement", "market", "skin", app.Name, "general", "chat", "bot"
                });

                var fields = new Dictionary<string, string>();
                foreach (var marketItem in newMarketItems)
                {
                    var storeItem = db.SteamStoreItems.FirstOrDefault(x => x.DescriptionId == marketItem.DescriptionId);
                    var description = marketItem.Description?.ItemType;
                    if (string.IsNullOrEmpty(description))
                    {
                        description = marketItem.Description?.Description ?? marketItem.SteamId;
                    }
                    if (storeItem != null)
                    {
                        var estimateSales = string.Empty;
                        if (storeItem.TotalSalesMax == null && storeItem.TotalSalesMin > 0)
                        {
                            estimateSales = $"{storeItem.TotalSalesMin.Value.ToQuantityString()} or more";
                        }
                        else if (storeItem.TotalSalesMin == storeItem.TotalSalesMax && storeItem.TotalSalesMin > 0)
                        {
                            estimateSales = $"{storeItem.TotalSalesMin.Value.ToQuantityString()}";
                        }
                        else if (storeItem.TotalSalesMin > 0 && storeItem.TotalSalesMax > 0)
                        {
                            estimateSales = $"{storeItem.TotalSalesMin.Value.ToQuantityString()} - {storeItem.TotalSalesMax.Value.ToQuantityString()}";
                        }
                        if (!string.IsNullOrEmpty(estimateSales))
                        {
                            description = $"{estimateSales} estimated sales";
                        }
                    }
                    fields.Add(marketItem.Description.Name, description);
                }

                var itemImageIds = newMarketItems
                    .Where(x => x.Description?.IconId != null)
                    .Select(x => x.Description.IconId);

                await _commandProcessor.ProcessAsync(new SendDiscordMessageRequest()
                {
                    GuidId = ulong.Parse(guild.DiscordId),
                    ChannelPatterns = guildChannels?.ToArray(),
                    Message = null,
                    Title = $"{app?.Name} Market - New Listings",
                    Description = $"{newMarketItems.Count()} new item(s) have just appeared in the {app?.Name} marketplace.",
                    Fields = fields,
                    FieldsInline = true,
                    Url = $"{_configuration.GetWebsiteUrl()}/items",
                    ThumbnailUrl = app?.IconUrl,
                    ImageUrl = (thumbnailImage != null ? $"{_configuration.GetWebsiteUrl()}/api/image/{thumbnailImage.Id}.{thumbnailImage.MimeType.GetFileExtension()}" : null),
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
