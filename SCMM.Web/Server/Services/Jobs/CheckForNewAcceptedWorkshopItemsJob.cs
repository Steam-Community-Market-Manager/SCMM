using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Discord.Client;
using SCMM.Steam.Client;
using SCMM.Steam.Shared;
using SCMM.Steam.Shared.Community.Requests.Html;
using SCMM.Web.Server.Configuration;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain;
using SCMM.Web.Server.Services.Jobs.CronJob;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs
{
    public class CheckForNewAcceptedWorkshopItemsJob : CronJobService
    {
        private readonly ILogger<CheckForNewAcceptedWorkshopItemsJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SteamConfiguration _steamConfiguration;

        public CheckForNewAcceptedWorkshopItemsJob(IConfiguration configuration, ILogger<CheckForNewAcceptedWorkshopItemsJob> logger, IServiceScopeFactory scopeFactory)
            : base(logger, configuration.GetJobConfiguration<CheckForNewAcceptedWorkshopItemsJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _steamConfiguration = configuration.GetSteamConfiguration();
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var discord = scope.ServiceProvider.GetRequiredService<DiscordClient>();
                var commnityClient = scope.ServiceProvider.GetService<SteamCommunityClient>();
                var service = scope.ServiceProvider.GetRequiredService<SteamService>();
                var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();

                var steamApps = await db.SteamApps.ToListAsync();
                if (!steamApps.Any())
                {
                    return;
                }

                foreach (var app in steamApps)
                {
                    _logger.LogInformation($"Checking for new accepted workshop items (appId: {app.SteamId})");
                    var workshopPage = await commnityClient.GetItemWorkshopPage(new SteamWorkshopBrowsePageRequest()
                    {
                        AppId = app.SteamId,
                        BrowseSort = SteamWorkshopBrowsePageRequest.BrowseSortAccepted
                    });
                    if (workshopPage == null)
                    {
                        _logger.LogError("Failed to get item workshop page");
                        continue;
                    }

                    // Check the "accepted" and get the most recent workshop file ids
                    var workshopFileIds = new List<string>();
                    var workshopItemElements = workshopPage.Descendants()
                        .Where(x => x.Attribute("class")?.Value == SteamConstants.SteamWorkshopItemClass);
                    foreach (var workshopItemElement in workshopItemElements)
                    {
                        var workshopItemPublishedFileId = workshopItemElement.Descendants()
                            .Where(x => x.Name.LocalName == "a")
                            .Select(x => x.Attribute(SteamConstants.SteamWorkshopItemPublishedFileIdAttribute)?.Value)
                            .FirstOrDefault(x => !String.IsNullOrEmpty(x));
                        if (workshopItemPublishedFileId != null)
                        {
                            workshopFileIds.Add(workshopItemPublishedFileId);
                        }
                    }

                    // Check if any of these workshop file ids are missing from the database
                    if (workshopFileIds.Any())
                    {
                        workshopFileIds = workshopFileIds.Take(30).ToList(); // only check the first 30, there shouldn't be more than this 
                        var missingWorkshopFileIds = workshopFileIds.Except(
                            db.SteamAssetWorkshopFiles
                                .Where(x => workshopFileIds.Contains(x.SteamId))
                                .Select(x => x.SteamId)
                                .ToList()
                        );

                        // We've found newly accepted workshop files, add them to the database
                        if (missingWorkshopFileIds.Any())
                        {
                            var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_steamConfiguration.ApplicationKey);
                            var steamRemoteStorage = steamWebInterfaceFactory.CreateSteamWebInterface<SteamRemoteStorage>();
                            var response = await steamRemoteStorage.GetPublishedFileDetailsAsync(missingWorkshopFileIds.Select(x => UInt64.Parse(x)).ToList());
                            if (response?.Data?.Any() != true)
                            {
                                _logger.LogError("Failed to get published file details");
                                continue;
                            }

                            foreach (var publishedFile in response.Data)
                            {
                                var workshopFile = await service.AddOrUpdateAssetWorkshopFile(app, publishedFile.PublishedFileId.ToString());
                                if (workshopFile != null)
                                {
                                    await discord.BroadcastMessage(
                                        channelPattern: $"announcement|workshop|store|skin|{app.Name}",
                                        message: null,
                                        title: $"{publishedFile.Title} has been accepted!",
                                        description: $"This workshop item was just accepted in-game and should appear on the {app.Name} store shortly.",
                                        url: new SteamWorkshopViewFileDetailsRequest()
                                        {
                                            Id = publishedFile.PublishedFileId.ToString()
                                        }.Uri.ToString(),
                                        thumbnailUrl: app.IconUrl,
                                        imageUrl: publishedFile.PreviewUrl.ToString(),
                                        color: ColorTranslator.FromHtml(app.PrimaryColor)                                       
                                    );
                                }
                            }

                            db.SaveChanges();
                        }
                    }
                }
            }
        }

    }
}
