using CommandQuery;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.API.Commands;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store;
using SCMM.Shared.Web.Extensions;
using SCMM.Steam.API;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Job.Server.Jobs.Cron;

namespace SCMM.Steam.Job.Server.Jobs
{
    public class CheckForNewMarketItemsJob : CronJobService
    {
        private readonly ILogger<CheckForNewMarketItemsJob> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;

        public CheckForNewMarketItemsJob(IConfiguration configuration, ILogger<CheckForNewMarketItemsJob> logger, IServiceScopeFactory scopeFactory)
            : base(logger, configuration.GetJobConfiguration<CheckForNewMarketItemsJob>())
        {
            _logger = logger;
            _configuration = configuration;
            _scopeFactory = scopeFactory;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var commandProcessor = scope.ServiceProvider.GetRequiredService<ICommandProcessor>();
            var queryProcessor = scope.ServiceProvider.GetRequiredService<IQueryProcessor>();
            var steamCommnityClient = scope.ServiceProvider.GetService<SteamCommunityWebClient>();
            var steamService = scope.ServiceProvider.GetService<SteamService>();
            var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();

            var assetDescriptions = db.SteamAssetDescriptions
                .Where(x => x.MarketItem == null && (x.IsMarketable || x.MarketableRestrictionDays > 0))
                .Where(x => !x.IsSpecialDrop && !x.IsTwitchDrop)
                .Where(x => x.TimeAccepted != null)
                .Include(x => x.App)
                .ToList();
            if (!assetDescriptions.Any())
            {
                return;
            }

            var usdCurrency = db.SteamCurrencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyUSD);
            if (usdCurrency == null)
            {
                return;
            }

            // TODO: Check up to 5 times with a 1min delay between each attempt

            _logger.LogInformation($"Checking for new market items (assets: {assetDescriptions.Count})");
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

                    var marketPriceOverviewResponse = await steamCommnityClient.GetMarketPriceOverview(marketPriceOverviewRequest);
                    if (marketPriceOverviewResponse?.Success == true)
                    {
                        var newMarketItem = await steamService.AddOrUpdateMarketItem(assetDescription.App, usdCurrency, marketPriceOverviewResponse, assetDescription);
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
                        _logger.LogError(ex, $"Failed to check new market item (classId: {assetDescription.ClassId}). {ex.Message}");
                    }
                }
            }

            if (newMarketItems.Any())
            {
                var thumbnailExpiry = DateTimeOffset.Now.AddDays(90);
                var thumbnail = await GenerateMarketItemsThumbnailImage(queryProcessor, newMarketItems, thumbnailExpiry);
                if (thumbnail != null)
                {
                    db.FileData.Add(thumbnail);
                }

                db.SaveChanges();

                await BroadcastNewMarketItemsNotification(commandProcessor, db, newMarketItems, thumbnail);
            }
        }

        private async Task<FileData> GenerateMarketItemsThumbnailImage(IQueryProcessor queryProcessor, IEnumerable<SteamMarketItem> marketItems, DateTimeOffset expiresOn)
        {
            var items = marketItems.OrderBy(x => x.Description?.Name);
            var itemImageSources = items
                .Where(x => x.Description != null)
                .Select(x => new ImageSource()
                {
                    Title = x.Description.Name,
                    ImageUrl = x.Description.IconUrl,
                    ImageData = x.Description.Icon?.Data,
                })
                .ToList();

            var thumbnail = await queryProcessor.ProcessAsync(new GetImageMosaicRequest()
            {
                ImageSources = itemImageSources,
                TileSize = 256,
                Columns = 3
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

        private async Task BroadcastNewMarketItemsNotification(ICommandProcessor commandProcessor, SteamDbContext db, IEnumerable<SteamMarketItem> newMarketItems, FileData thumbnailImage)
        {
            newMarketItems = newMarketItems?.OrderBy(x => x.Description.Name);
            var guilds = db.DiscordGuilds.Include(x => x.Configurations).ToList();
            foreach (var guild in guilds)
            {
                if (guild.IsSet(Steam.Data.Store.DiscordConfiguration.Alerts) && !guild.Get(Steam.Data.Store.DiscordConfiguration.Alerts).Value.Contains(Steam.Data.Store.DiscordConfiguration.AlertsMarket))
                {
                    continue;
                }

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

                var app = newMarketItems.Where(x => x.App != null).FirstOrDefault()?.App;
                await commandProcessor.ProcessAsync(new SendDiscordMessageRequest()
                {
                    GuidId = ulong.Parse(guild.DiscordId),
                    ChannelPatterns = new[] {
                        guild.Get(Steam.Data.Store.DiscordConfiguration.AlertChannel).Value, "announcement", "market", "skin", app.Name, "general", "chat", "bot"
                    },
                    Message = null,
                    Title = $"{app?.Name} Market - New Listings",
                    Description = $"{newMarketItems.Count()} new item(s) have just appeared in the {app?.Name} marketplace.",
                    Fields = fields,
                    FieldsInline = true,
                    Url = $"{_configuration.GetWebsiteUrl()}/items",
                    ThumbnailUrl = app?.IconUrl,
                    ImageUrl = $"{_configuration.GetWebsiteUrl()}/api/image/{thumbnailImage?.Id}",
                    Colour = app?.PrimaryColor
                });
            }
        }
    }
}
