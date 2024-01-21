using CommandQuery;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.API.Events;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Models.WebApi.Requests.ISteamRemoteStorage;
using SCMM.Steam.Data.Store;
using System.Xml.Linq;

namespace SCMM.Steam.Functions.Timer;

public class CheckForNewWorkshopFiles
{
    private readonly SteamConfiguration _steamConfiguration;
    private readonly SteamDbContext _steamDb;
    private readonly SteamWebApiClient _steamWebApiClient;
    private readonly SteamCommunityWebClient _steamCommunityClient;
    private readonly ICommandProcessor _commandProcessor;
    private readonly IQueryProcessor _queryProcessor;
    private readonly IServiceBus _serviceBus;

    public CheckForNewWorkshopFiles(ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, SteamConfiguration steamConfiguration, SteamDbContext steamDb, SteamWebApiClient steamWebApiClient, SteamCommunityWebClient steamCommunityClient, IServiceBus serviceBus)
    {
        _commandProcessor = commandProcessor;
        _queryProcessor = queryProcessor;
        _steamConfiguration = steamConfiguration;
        _steamDb = steamDb;
        _steamWebApiClient = steamWebApiClient;
        _steamCommunityClient = steamCommunityClient;
        _serviceBus = serviceBus;
    }

    [Function("Check-New-Workshop-Files")]
    public async Task Run([TimerTrigger("0 15/30  * * * *")] /* every 30 minutes, starting at 15 minutes past the hour */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Check-New-Workshop-Files");
        var deepScan = false; // TODO: Make configurable?

        var apps = await _steamDb.SteamApps.AsNoTracking()
            .Where(x => (x.FeatureFlags & SteamAppFeatureFlags.ItemWorkshopSubmissionTracking) != 0)
            .ToListAsync();

        var assetDescriptions = await _steamDb.SteamAssetDescriptions.AsNoTracking()
            .Where(x => x.CreatorId != null)
            .Select(x => new
            {
                Id = x.Id,
                AppId = x.AppId,
                CreatorId = x.CreatorId,
                WorkshopFileId = x.WorkshopFileId,
                ItemName = x.Name,
                ItemCollection = x.ItemCollection,
                TimeAccepted = x.TimeAccepted
            })
            .ToListAsync();

        var creators = assetDescriptions
            .GroupBy(x => new { x.AppId, x.CreatorId })
            .Select(x => x.Key)
            .ToArray();

        // Check all unique accepted creators for missing workshop files
        foreach (var creator in creators)
        {
            var app = apps.FirstOrDefault(x => x.Id == creator.AppId);
            var publishedFiles = new Dictionary<ulong, string>();
            var workshopHtml = (XElement)null;
            try
            {
                workshopHtml = await _steamCommunityClient.GetHtmlAsync(new SteamProfileMyWorkshopFilesPageRequest()
                {
                    SteamId = creator.CreatorId.ToString(),
                    AppId = app.SteamId
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to get workshop files for creator id {creator.CreatorId}");
                continue;
            }

            // Get latest workshop file ids
            var paginingControls = workshopHtml.Descendants("div").FirstOrDefault(x => x.Attribute("class")?.Value == "workshopBrowsePagingControls");
            var lastPageLink = paginingControls?.Descendants("a").LastOrDefault(x => x.Attribute("class")?.Value == "pagelink");
            var pages = (deepScan ? int.Parse(lastPageLink?.Value ?? "1") : 1);
            for (int page = 1; page <= pages; page++)
            {
                if (page != 1)
                {
                    try
                    {
                        workshopHtml = await _steamCommunityClient.GetHtmlAsync(new SteamProfileMyWorkshopFilesPageRequest()
                        {
                            SteamId = creator.CreatorId.ToString(),
                            AppId = app.SteamId,
                            Page = page
                        });
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Failed to get workshop files for creator id {creator.CreatorId} (page: {page})");
                        continue;
                    }
                }

                var workshopItems = workshopHtml.Descendants("div").Where(x => x.Attribute("class")?.Value == "workshopItem").ToList();
                foreach (var workshopItem in workshopItems)
                {
                    var workshopItemLink = workshopItem.Descendants("a").FirstOrDefault();
                    var workshopItemTitle = workshopItem.Descendants("div").FirstOrDefault(x => x.Attribute("class")?.Value?.Contains("workshopItemTitle") == true);
                    if (workshopItemLink != null && workshopItemTitle != null)
                    {
                        publishedFiles[UInt64.Parse(workshopItemLink?.Attribute("data-publishedfileid").Value)] = workshopItemTitle.Value;
                    }
                }
            }
            if (!publishedFiles.Any())
            {
                continue;
            }

            // Get existing workshop files
            var publishedFileIds = publishedFiles.Select(x => x.Key.ToString());
            var existingWorkshopFileIds = await _steamDb.SteamWorkshopFiles
                .Where(x => publishedFileIds.Contains(x.SteamId))
                .Select(x => x.SteamId)
                .ToListAsync();

            // Get missing workshop files
            var missingPublishedFileIds = publishedFileIds.Except(existingWorkshopFileIds).ToArray();
            if (!missingPublishedFileIds.Any())
            {
                continue;
            }
            var missingPublishedFileDetails = await _steamWebApiClient.SteamRemoteStorageGetPublishedFileDetailsAsync(new GetPublishedFileDetailsJsonRequest()
            {
                PublishedFileIds = missingPublishedFileIds.Select(x => UInt64.Parse(x)).ToArray()
            });
            if (missingPublishedFileDetails?.PublishedFileDetails == null)
            {
                continue;
            }

            // Import missing workshop files
            foreach (var missingPublishedFile in missingPublishedFileDetails.PublishedFileDetails)
            {
                // Skip items which have very short placeholder sounding names or that have only just been published in the last few minutes.
                // Creators will often publish items temporarily just to test them in-game and then delete them again, which we don't want to import (yet).
                // If the item has existed for at least 15 minutes, we assume the creator is happy with the item and plans to keep it published
                if ((missingPublishedFile.Title.Length < 5) || missingPublishedFile.Title.StartsWith("test", StringComparison.InvariantCultureIgnoreCase) ||
                    (DateTimeOffset.UtcNow - missingPublishedFile.TimeCreated.SteamTimestampToDateTimeOffset()) <= TimeSpan.FromMinutes(30))
                {
                    continue;
                }

                // Skip items which are not skins
                if (missingPublishedFile.Tags.Select(x => x.Tag)?.Contains(Constants.SteamWorkshopTagSkin) != true)
                {
                    continue;
                }

                var workshopFile = new SteamWorkshopFile()
                {
                    AppId = app.Id,
                    CreatorId = creator.CreatorId
                };

                var assetDescription = assetDescriptions.FirstOrDefault(x => x.WorkshopFileId == missingPublishedFile.PublishedFileId);
                if (assetDescription != null)
                {
                    workshopFile.DescriptionId = assetDescription.Id;
                    workshopFile.TimeAccepted = assetDescription.TimeAccepted;
                    workshopFile.IsAccepted = true;
                }

                // Find existing item collections that this item belongs to
                var existingItemCollections = assetDescriptions
                    .Where(x => x.AppId == creator.AppId && x.CreatorId == creator.CreatorId)
                    .Where(x => !String.IsNullOrEmpty(x.ItemCollection))
                    .Select(x => x.ItemCollection)
                    .Distinct()
                    .ToArray();
                foreach (var existingItemCollection in existingItemCollections.OrderByDescending(x => x.Length))
                {
                    var isCollectionMatch = existingItemCollection
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .All(x => missingPublishedFile.Title.Contains(x, StringComparison.InvariantCultureIgnoreCase));
                    if (isCollectionMatch)
                    {
                        workshopFile.ItemCollection = existingItemCollection;
                        break;
                    }
                }

                // TODO: Detect new collections

                var updatedWorkshopItem = await _commandProcessor.ProcessWithResultAsync(new UpdateSteamWorkshopFileRequest()
                {
                    WorkshopFile = workshopFile,
                    PublishedFile = missingPublishedFile
                });

                if (workshopFile.IsTransient)
                {
                    _steamDb.SteamWorkshopFiles.Add(workshopFile);
                    if (workshopFile.TimeCreated != null && (DateTimeOffset.Now - workshopFile.TimeCreated.Value).TotalDays <= 7)
                    {
                        await _serviceBus.SendMessageAsync(new WorkshopFilePublishedMessage()
                        {
                            AppId = UInt64.Parse(app.SteamId),
                            AppName = app.Name,
                            AppIconUrl = app.IconUrl,
                            AppColour = app.PrimaryColor,
                            CreatorId = workshopFile.CreatorId ?? 0,
                            CreatorName = workshopFile.CreatorProfile?.Name,
                            CreatorAvatarUrl = workshopFile.CreatorProfile?.AvatarUrl,
                            ItemId = UInt64.Parse(workshopFile.SteamId),
                            ItemType = workshopFile.ItemType,
                            ItemShortName = workshopFile.ItemShortName,
                            ItemName = workshopFile.Name,
                            ItemDescription = workshopFile.Description,
                            ItemCollection = workshopFile.ItemCollection,
                            ItemImageUrl = workshopFile.PreviewUrl,
                        });
                    }
                }
            }

            await _steamDb.SaveChangesAsync();
        }
    }
}