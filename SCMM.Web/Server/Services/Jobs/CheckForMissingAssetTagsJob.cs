using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Steam.Shared;
using SCMM.Web.Server.Configuration;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain;
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
    public class CheckForMissingAssetTagsJob : CronJobService
    {
        private readonly ILogger<CheckForMissingAssetTagsJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SteamConfiguration _steamConfiguration;

        public CheckForMissingAssetTagsJob(IConfiguration configuration, ILogger<CheckForMissingAssetTagsJob> logger, IServiceScopeFactory scopeFactory)
            : base(logger, configuration.GetJobConfiguration<CheckForMissingAssetTagsJob>())
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

                var assetDescriptionsWithMissingTags = db.SteamAssetDescriptions
                    .Where(x => !x.Tags.Serialised.Contains(SteamConstants.SteamAssetTagCategory))
                    .Include(x => x.App)
                    .ToList();

                if (!assetDescriptionsWithMissingTags.Any())
                {
                    return;
                }

                var language = db.SteamLanguages.FirstOrDefault(x => x.IsDefault);
                if (language == null)
                {
                    return;
                }

                var groupedAssetDescriptions = assetDescriptionsWithMissingTags.GroupBy(x => x.App);
                foreach (var group in groupedAssetDescriptions)
                {
                    var assetClassIds = group.Select(x => UInt64.Parse(x.SteamId)).ToList();
                    foreach (var batch in assetClassIds.Batch(100)) // Batch to 100 per request to avoid server ban
                    {
                        _logger.LogInformation($"Checking for missing asset tags (ids: {batch.Count()})");
                        var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_steamConfiguration.ApplicationKey);
                        var steamEconomy = steamWebInterfaceFactory.CreateSteamWebInterface<SteamEconomy>();
                        var response = await steamEconomy.GetAssetClassInfoAsync(UInt32.Parse(group.Key.SteamId), batch.ToList(), language.SteamId);
                        if (response?.Data?.Success != true)
                        {
                            _logger.LogError("Failed to get asset class info");
                            continue;
                        }

                        var assetDescriptionsJoined = response.Data.AssetClasses.Join(group,
                            x => x.ClassId.ToString(),
                            y => y.SteamId,
                            (x, y) => new
                            {
                                AssetDescription = y,
                                AssetClass = x
                            }
                        );

                        foreach (var item in assetDescriptionsJoined)
                        {
                            steamService.UpdateAssetDescription(
                                item.AssetDescription, item.AssetClass
                            );
                        }

                        await db.SaveChangesAsync();
                    }
                }
            }
        }
    }
}
