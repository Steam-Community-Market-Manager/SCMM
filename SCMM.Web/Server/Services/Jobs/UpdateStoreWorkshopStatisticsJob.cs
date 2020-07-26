using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Steam.Shared;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain;
using SCMM.Web.Server.Services.Jobs.CronJob;
using SCMM.Web.Shared;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs
{
    public class UpdateStoreWorkshopStatisticsJob : CronJobService
    {
        private readonly ILogger<UpdateStoreWorkshopStatisticsJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SteamConfiguration _steamConfiguration;

        public UpdateStoreWorkshopStatisticsJob(IConfiguration configuration, ILogger<UpdateStoreWorkshopStatisticsJob> logger, IServiceScopeFactory scopeFactory)
            : base(logger, configuration.GetJobConfiguration<UpdateStoreWorkshopStatisticsJob>())
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

                var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
                var assetDescriptions = db.SteamStoreItems
                    .Where(x => x.Description.WorkshopFile.SteamId != null)
                    .Where(x => x.Description.WorkshopFile.AcceptedOn == x.App.StoreItems.Max(x => x.Description.WorkshopFile.AcceptedOn))
                    .Include(x => x.Description.WorkshopFile)
                    .Select(x => x.Description)
                    .ToList();

                var workshopFileIds = assetDescriptions.Select(x => UInt64.Parse(x.WorkshopFile.SteamId)).ToList();
                foreach (var batch in workshopFileIds.Batch(100)) // Batch to 100 per request to avoid server ban
                {
                    _logger.LogInformation($"Updating store item workshop statistics (ids: {batch.Count()})");
                    var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_steamConfiguration.ApplicationKey);
                    var steamRemoteStorage = steamWebInterfaceFactory.CreateSteamWebInterface<SteamRemoteStorage>();
                    var response = await steamRemoteStorage.GetPublishedFileDetailsAsync(batch.ToList());
                    if (response?.Data?.Any() != true)
                    {
                        // TODO: Log this
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
                            item.AssetDescription, item.PublishedFile, updateSubscriptionGraph: true
                        );
                    }

                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
