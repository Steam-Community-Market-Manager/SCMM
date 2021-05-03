using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Discord.Client;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Store;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using SCMM.Steam.Job.Server.Jobs.Cron;
using SCMM.Steam.API.Queries;
using SCMM.Steam.API;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Store;
using SCMM.Shared.Web.Extensions;
using SCMM.Discord.API.Commands;

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
            var steamCommnityClient = scope.ServiceProvider.GetService<SteamCommunityClient>();
            var steamService = scope.ServiceProvider.GetRequiredService<SteamService>();
            var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();

            var steamApps = db.SteamApps.ToList();
            if (!steamApps.Any())
            {
                return;
            }

            var language = db.SteamLanguages.FirstOrDefault(x => x.IsDefault);
            if (language == null)
            {
                return;
            }

            var currency = db.SteamCurrencies.FirstOrDefault(x => x.IsDefault);
            if (currency == null)
            {
                return;
            }

            var pageRequests = new List<SteamMarketSearchPaginatedJsonRequest>();
            foreach (var app in steamApps)
            {
                var appPageCountRequest = new SteamMarketSearchPaginatedJsonRequest()
                {
                    AppId = app.SteamId,
                    Start = 1,
                    Count = 1,
                    Language = language.SteamId,
                    CurrencyId = currency.SteamId,
                    SortColumn = SteamMarketSearchPaginatedJsonRequest.SortColumnName
                };

                _logger.LogInformation($"Checking for new market items (appId: {app.SteamId})");
                var appPageCountResponse = await steamCommnityClient.GetMarketSearchPaginated(appPageCountRequest);
                if (appPageCountResponse?.Success != true || appPageCountResponse?.TotalCount <= 0)
                {
                    continue;
                }

                var total = appPageCountResponse.TotalCount;
                var pageSize = SteamMarketSearchPaginatedJsonRequest.MaxPageSize;
                var appPageRequests = new List<SteamMarketSearchPaginatedJsonRequest>();
                for (var i = 0; i <= total; i += pageSize)
                {
                    appPageRequests.Add(
                        new SteamMarketSearchPaginatedJsonRequest()
                        {
                            AppId = app.SteamId,
                            Start = i,
                            Count = Math.Min(total - i, pageSize),
                            Language = language.SteamId,
                            CurrencyId = currency.SteamId,
                            SortColumn = SteamMarketSearchPaginatedJsonRequest.SortColumnName
                        }
                    );
                }

                if (appPageRequests.Any())
                {
                    pageRequests.AddRange(appPageRequests);
                }

                // Add a 10 second delay between requests to avoid "Too Many Requests" error
                var newMarketItems = await Observable.Interval(TimeSpan.FromSeconds(10))
                    .Zip(pageRequests, (x, y) => y)
                    .Select(x => Observable.FromAsync(() =>
                    {
                        _logger.LogInformation($"Checking for new market items (appId: {x.AppId}, start: {x.Start}, end: {x.Start + x.Count})");
                        return steamCommnityClient.GetMarketSearchPaginated(x);
                    }))
                    .Merge()
                    .Where(x => x?.Success == true && x?.Results?.Count > 0)
                    .SelectMany(x =>
                    {
                        var tasks = steamService.FindOrAddSteamMarketItems(x.Results, currency);
                        Task.WaitAll(tasks);
                        return tasks.Result;
                    })
                    .Where(x => x?.IsTransient == true)
                    .Where(x => x?.App != null && x?.Description != null)
                    .ToList();

                if (newMarketItems.Any())
                {
                    var thumbnailExpiry = DateTimeOffset.Now.AddDays(90);
                    var thumbnail = await GenerateMarketItemsThumbnail(queryProcessor, newMarketItems, thumbnailExpiry);
                    if (thumbnail != null)
                    {
                        db.ImageData.Add(thumbnail);
                    }

                    db.SaveChanges();

                    await BroadcastNewMarketItemsNotification(commandProcessor, db, app, newMarketItems, thumbnail);
                }
            }
        }

        private async Task<ImageData> GenerateMarketItemsThumbnail(IQueryProcessor queryProcessor, IEnumerable<SteamMarketItem> marketItems, DateTimeOffset expiresOn)
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

            return new ImageData()
            {
                Data = thumbnail.Data,
                MimeType = thumbnail.MimeType,
                ExpiresOn = expiresOn
            };
        }

        private async Task BroadcastNewMarketItemsNotification(ICommandProcessor commandProcessor, SteamDbContext db, SteamApp app, IEnumerable<SteamMarketItem> newMarketItems, ImageData thumbnail)
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
                    var description = marketItem.Description?.Tags?.GetItemType(marketItem.Description?.Name);
                    if (String.IsNullOrEmpty(description))
                    {
                        description = marketItem.Description?.Description ?? marketItem.SteamId;
                    }
                    if (storeItem != null)
                    {
                        var estimateSales = String.Empty;
                        if (storeItem.TotalSalesMax == null && storeItem.TotalSalesMin > 0)
                        {
                            estimateSales = $"{storeItem.TotalSalesMin.ToQuantityString()} or more";
                        }
                        else if (storeItem.TotalSalesMin == storeItem.TotalSalesMax && storeItem.TotalSalesMin > 0)
                        {
                            estimateSales = $"{storeItem.TotalSalesMin.ToQuantityString()}";
                        }
                        else if (storeItem.TotalSalesMin > 0 && storeItem.TotalSalesMax > 0)
                        {
                            estimateSales = $"{storeItem.TotalSalesMin.ToQuantityString()} - {storeItem.TotalSalesMax.Value.ToQuantityString()}";
                        }
                        if (!String.IsNullOrEmpty(estimateSales))
                        {
                            description = $"{estimateSales} estimated sales";
                        }
                    }
                    fields.Add(marketItem.Description.Name, description);
                }

                var itemImageIds = newMarketItems
                    .Where(x => x.Description?.IconId != null)
                    .Select(x => x.Description.IconId);

                await commandProcessor.ProcessAsync(new SendDiscordMessageRequest()
                {
                    GuildPattern = guild.DiscordId,
                    ChannelPattern = guild.Get(Steam.Data.Store.DiscordConfiguration.AlertChannel, $"announcement|market|skin|{app.Name}").Value,
                    Message = null,
                    Title = $"{app.Name} Market - New Listings",
                    Description = $"{newMarketItems.Count()} new item(s) have just appeared in the {app.Name} marketplace.",
                    Fields = fields,
                    FieldsInline = true,
                    Url = $"{_configuration.GetWebsiteUrl()}/steam/marketlistings",
                    ThumbnailUrl = app.IconUrl,
                    ImageUrl = $"{_configuration.GetWebsiteUrl()}/api/image/{thumbnail?.Id}",
                    Colour = app.PrimaryColor
                });
            }
        }
    }
}
