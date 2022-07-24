using CommandQuery;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Discord.API.Commands;
using SCMM.Shared.API.Extensions;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.WebApi.Models;
using SCMM.Steam.Data.Models.WebApi.Requests.IPublishedFileService;
using SCMM.Steam.Data.Store;
using System.Globalization;
using System.Text;

namespace SCMM.Steam.Functions.Timer;

public class CheckForNewAcceptedWorkshopItems
{
    private readonly IConfiguration _configuration;
    private readonly SteamDbContext _db;
    private readonly ICommandProcessor _commandProcessor;
    private readonly IQueryProcessor _queryProcessor;
    private readonly SteamWebApiClient _apiClient;

    public CheckForNewAcceptedWorkshopItems(IConfiguration configuration, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, SteamDbContext db, SteamWebApiClient apiClient)
    {
        _configuration = configuration;
        _commandProcessor = commandProcessor;
        _queryProcessor = queryProcessor;
        _db = db;
        _apiClient = apiClient;
    }

    // TODO: Revisit this. It doesn't provide any extra value when compared to CheckForNewItemDefinitions 
    // [Function("Check-New-Accepted-Workshop-Items")]
    public async Task Run([TimerTrigger("30 * * * * *")] /* every minute */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Check-New-Accepted-Workshop-Items");

        var steamApps = await _db.SteamApps
            .Where(x => x.Features.HasFlag(SteamAppFeatureTypes.ItemWorkshop))
            .Where(x => x.MostRecentlyAcceptedWorkshopFileId > 0)
            .Where(x => x.IsActive)
            .ToListAsync();
        if (!steamApps.Any())
        {
            return;
        }

        foreach (var app in steamApps)
        {
            logger.LogTrace($"Checking for new accepted workshop items (appId: {app.SteamId})");

            var queryResults = await _apiClient.PublishedFileServiceQueryFiles(new QueryFilesJsonRequest()
            {
                QueryType = QueryFilesJsonRequest.QueryTypeAcceptedForGameRankedByAcceptanceDate,
                AppId = UInt64.Parse(app.SteamId),
                Page = 0,
                NumPerPage = 30,
                ReturnShortDescription = true
            });

            var newlyAcceptedItems = new List<PublishedFileDetails>();
            foreach (var item in queryResults.PublishedFileDetails.Where(x => x.WorkshopAccepted))
            {
                if (item.PublishedFileId == app.MostRecentlyAcceptedWorkshopFileId)
                {
                    break;
                }
                else
                {
                    newlyAcceptedItems.Add(item);
                }
            }

            if (newlyAcceptedItems.Any())
            {
                app.MostRecentlyAcceptedWorkshopFileId = newlyAcceptedItems.First().PublishedFileId;
                await _db.SaveChangesAsync();

                await BroadcastNewAcceptedWorkshopItemsNotification(logger, app, newlyAcceptedItems);
            }
        }
    }

    private async Task BroadcastNewAcceptedWorkshopItemsNotification(ILogger logger, SteamApp app, IEnumerable<PublishedFileDetails> newWorkshopItems)
    {
        var guilds = _db.DiscordGuilds.Include(x => x.Configuration).ToList();
        foreach (var guild in guilds)
        {
            try
            {
                if (!bool.Parse(guild.Get(DiscordConfiguration.AlertsWorkshop, Boolean.FalseString).Value))
                {
                    continue;
                }

                var guildChannels = guild.List(DiscordConfiguration.AlertChannel).Value?.Union(new[] {
                    "announcement", "workshop", "skin", app.Name, "general", "chat", "bot"
                });

                var description = new StringBuilder();
                var fields = new Dictionary<string, string>();

                description.Append($"{newWorkshopItems.Count()} new item(s) have just been accepted in-game to {app?.Name} from the Steam workshop.");
                foreach (var item in newWorkshopItems.Reverse())
                {
                    var workshopUrl = new SteamWorkshopFileDetailsPageRequest()
                    {
                        Id = item.PublishedFileId.ToString()
                    };
                    fields.Add(
                        $"🆕 {item.PublishedFileId}",
                        $"[{item.Title}]({workshopUrl.ToString()})"
                    );
                }

                if (fields.Count > 24)
                {
                    fields = fields.Take(24).ToDictionary(x => x.Key, x => x.Value);
                    fields.Add(
                        $"+{newWorkshopItems.Count() - 24} items",
                        "View full item list for more details"
                    );
                }

                await _commandProcessor.ProcessAsync(new SendDiscordMessageRequest()
                {
                    GuidId = ulong.Parse(guild.DiscordId),
                    ChannelPatterns = guildChannels?.ToArray(),
                    Message = null,
                    Title = $"{app?.Name} Workshop - New Accepted Items",
                    Description = description.ToString(),
                    Fields = fields,
                    FieldsInline = true,
                    Url = new SteamWorkshopBrowsePageRequest()
                    {
                        AppId = app.SteamId,
                        Section = SteamWorkshopBrowsePageRequest.BrowseSortAccepted
                    },
                    ThumbnailUrl = app?.IconUrl,
                    Colour = UInt32.Parse(app.PrimaryColor.Replace("#", ""), NumberStyles.HexNumber)
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to send new accepted workshop items notification to guild (id: {guild.Id})");
                continue;
            }
        }
    }
}