using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Extensions;
using SCMM.Web.Server.Services.Jobs.CronJob;
using SCMM.Web.Shared;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs
{
    public class UpdateAssetWorkshopFilesJob : CronJobService
    {
        private readonly ILogger<UpdateAssetWorkshopFilesJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SteamConfiguration _steamConfiguration;

        public UpdateAssetWorkshopFilesJob(IConfiguration configuration, ILogger<UpdateAssetWorkshopFilesJob> logger, IServiceScopeFactory scopeFactory)
            : base(logger, configuration.GetJobConfiguration<UpdateAssetWorkshopFilesJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _steamConfiguration = configuration.GetSteamConfiguration();
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var commnityClient = scope.ServiceProvider.GetService<SteamCommunityClient>();
                var steamService = scope.ServiceProvider.GetRequiredService<SteamService>();
                var db = scope.ServiceProvider.GetRequiredService<ScmmDbContext>();
                var assetDescriptions = db.SteamAssetDescriptions
                    .Where(x => x.WorkshopFile.SteamId != null)
                    .Include(x => x.WorkshopFile)
                    .ToList();

                var workshopFileIds = assetDescriptions.Select(x => UInt64.Parse(x.WorkshopFile.SteamId)).ToList();
                foreach (var batch in workshopFileIds.Batch(100)) // Batch to 100 per request to avoid server ban
                {
                    _logger.LogInformation($"Updating asset workshop information (ids: {batch.Count()})");
                    var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_steamConfiguration.ApplicationKey);
                    var steamRemoteStorage = steamWebInterfaceFactory.CreateSteamWebInterface<SteamRemoteStorage>();
                    var response = await steamRemoteStorage.GetPublishedFileDetailsAsync(batch.ToList());
                    if (response?.Data?.Any() != true)
                    {
                        _logger.LogError("Failed to get published file details");
                        continue;
                    }

                    var assetWorkshopJoined = response.Data.Join(assetDescriptions,
                        x => x.PublishedFileId.ToString(),
                        y => y.WorkshopFile.SteamId,
                        (x, y) => new
                        {
                            AssetDescription = y,
                            PublishedFile = x
                        }
                    );

                    foreach (var item in assetWorkshopJoined)
                    {
                        await steamService.UpdateAssetDescription(
                            item.AssetDescription, item.PublishedFile
                        );
                    }

                    db.SaveChanges();
                }
            }
        }
    }
}
