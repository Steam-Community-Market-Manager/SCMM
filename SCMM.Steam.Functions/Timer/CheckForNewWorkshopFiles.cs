using CommandQuery;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Azure.ServiceBus;
using SCMM.Shared.API.Messages;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Store;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System.Xml.Linq;

namespace SCMM.Steam.Functions.Timer;

public class CheckForNewWorkshopFiles
{
    private readonly SteamDbContext _steamDb;
    private readonly SteamConfiguration _steamCfg;
    private readonly SteamCommunityWebClient _steamCommunityClient;
    private readonly ICommandProcessor _commandProcessor;
    private readonly IQueryProcessor _queryProcessor;
    private readonly ServiceBusClient _serviceBus;

    public CheckForNewWorkshopFiles(ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, SteamDbContext steamDb, SteamConfiguration steamCfg, SteamCommunityWebClient steamCommunityClient, ServiceBusClient serviceBus)
    {
        _commandProcessor = commandProcessor;
        _queryProcessor = queryProcessor;
        _steamDb = steamDb;
        _steamCfg = steamCfg;
        _steamCommunityClient = steamCommunityClient;
        _serviceBus = serviceBus;
    }

    [Function("Check-New-Workshop-Files")]
    public async Task Run([TimerTrigger("0 15/30  * * * *")] /* every 30 minutes, starting at 15 minutes past the hour */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Check-New-Workshop-Files");
        var deepScan = false; // TODO: Make configurable?

        var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_steamCfg.ApplicationKey);
        var steamRemoteStorage = steamWebInterfaceFactory.CreateSteamWebInterface<SteamRemoteStorage>();

        var apps = await _steamDb.SteamApps.AsNoTracking()
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
                workshopHtml = await _steamCommunityClient.GetHtml(new SteamProfileMyWorkshopFilesPageRequest()
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
                        workshopHtml = await _steamCommunityClient.GetHtml(new SteamProfileMyWorkshopFilesPageRequest()
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
                        // Ignore items with very short placeholder names, likely the creator "testing" the item
                        if (workshopItemTitle.Value?.Length >= 5)
                        {
                            publishedFiles[UInt64.Parse(workshopItemLink?.Attribute("data-publishedfileid").Value)] = workshopItemTitle.Value;
                        }
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
            var missingPublishedFileDetails = await steamRemoteStorage.GetPublishedFileDetailsAsync(
                missingPublishedFileIds.Select(x => UInt64.Parse(x)).ToArray()
            );
            if (missingPublishedFileDetails?.Data == null)
            {
                continue;
            }

            // Import missing workshop files
            foreach (var missingPublishedFile in missingPublishedFileDetails.Data)
            {
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
                        .All(x => missingPublishedFile.Title.Contains(x));
                    if (isCollectionMatch)
                    {
                        workshopFile.ItemCollection = existingItemCollection;
                        break;
                    }
                }

                // Detect new collections
                // TODO: This...

                var updatedWorkshopItem = await _commandProcessor.ProcessWithResultAsync(new UpdateSteamWorkshopFileRequest()
                {
                    WorkshopFile = workshopFile,
                    PublishedFile = missingPublishedFile
                });

                if (workshopFile.IsTransient)
                {
                    _steamDb.SteamWorkshopFiles.Add(workshopFile);
                    await _serviceBus.SendMessageAsync(new WorkshopFilePublishedMessage()
                    {
                        Id = UInt64.Parse(workshopFile.SteamId),
                        AppId = UInt64.Parse(app.SteamId),
                        CreatorId = UInt64.Parse(workshopFile.CreatorProfile?.SteamId ?? "0"),
                        CreatorName = workshopFile.CreatorProfile?.Name,
                        CreatorAvatarUrl = workshopFile.CreatorProfile?.AvatarUrl,
                        ItemType = workshopFile.ItemType,
                        ItemShortName = workshopFile.ItemShortName,
                        ItemCollection = workshopFile.ItemCollection,
                        Name = workshopFile.Name,
                        Description = workshopFile.Description,
                        PreviewUrl = workshopFile.PreviewUrl,
                        TimeCreated = workshopFile.TimeCreated ?? DateTimeOffset.Now
                    });
                }
            }

            await _steamDb.SaveChangesAsync();
        }
    }
}