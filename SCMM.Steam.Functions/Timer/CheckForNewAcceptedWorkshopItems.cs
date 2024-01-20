using CommandQuery;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.API.Events;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.WebApi.Models;
using SCMM.Steam.Data.Models.WebApi.Requests.IPublishedFileService;
using SCMM.Steam.Data.Store;
using System.Linq;

namespace SCMM.Steam.Functions.Timer;

public class CheckForNewAcceptedWorkshopItems
{
    private readonly ICommandProcessor _commandProcessor;
    private readonly IQueryProcessor _queryProcessor;
    private readonly SteamDbContext _steamDb;
    private readonly IServiceBus _serviceBus;
    private readonly SteamWebApiClient _apiClient;

    public CheckForNewAcceptedWorkshopItems(ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, SteamDbContext steamDb, SteamWebApiClient apiClient, IServiceBus serviceBus)
    {
        _commandProcessor = commandProcessor;
        _queryProcessor = queryProcessor;
        _steamDb = steamDb;
        _apiClient = apiClient;
        _serviceBus = serviceBus;
    }

    [Function("Check-New-Accepted-Workshop-Items")]
    public async Task Run([TimerTrigger("0 0/3 * * * *")] /* every 3 minutes */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Check-New-Accepted-Workshop-Items");

        var steamApps = await _steamDb.SteamApps
            .Where(x => x.Features.HasFlag(SteamAppFeatureTypes.ItemWorkshop))
            .Where(x => x.MostRecentlyAcceptedWorkshopFileId > 0)
            .Where(x => x.IsActive)
            .ToListAsync();
        
        foreach (var app in steamApps)
        {
            logger.LogTrace($"Checking for new accepted workshop items (appId: {app.SteamId})");

            var queryResults = await _apiClient.PublishedFileServiceQueryFilesAsync(new QueryFilesJsonRequest()
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
                await _steamDb.SaveChangesAsync();

                var newWorkshopFileIds = newlyAcceptedItems.Select(x => x.PublishedFileId).ToArray();
                var existingItemsWithWorkshopFileIdsCount = await _steamDb.SteamAssetDescriptions
                    .Where(x => x.WorkshopFileId != null && newWorkshopFileIds.Contains(x.WorkshopFileId.Value))
                    .CountAsync();

                if (newWorkshopFileIds.Length > existingItemsWithWorkshopFileIdsCount)
                {
                    logger.LogInformation($"Found {newWorkshopFileIds.Length - existingItemsWithWorkshopFileIdsCount} new workshop items that are not in the database yet (appId: {app.SteamId})");
                    await _serviceBus.SendMessageAsync(new AppAcceptedWorkshopFilesUpdatedMessage()
                    {
                        AppId = UInt64.Parse(app.SteamId),
                        AppName = app.Name,
                        AppIconUrl = app.IconUrl,
                        AppColour = app.PrimaryColor,
                        AcceptedWorkshopFileIds = newlyAcceptedItems.Select(x => x.PublishedFileId).ToArray(),
                        ViewAcceptedWorkshopFilesPageUrl = new SteamWorkshopBrowsePageRequest()
                        {
                            AppId = app.SteamId,
                            Section = SteamWorkshopBrowsePageRequest.SectionAccepted,
                            BrowseSort = SteamWorkshopBrowsePageRequest.BrowseSortAccepted
                        },
                        TimeUpdated = DateTimeOffset.UtcNow,
                    });
                }
            }
        }
    }
}