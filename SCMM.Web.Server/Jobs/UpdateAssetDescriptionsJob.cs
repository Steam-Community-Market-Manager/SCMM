using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Data.Shared.Extensions;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Web.Server.Extensions;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SCMM.Web.Server.Jobs.CronJob;
using SCMM.Web.Server.Jobs;
using SCMM.Steam.API;

namespace SCMM.Web.Server.Jobs
{
    public class UpdateAssetDescriptionsJob : CronJobService
    {
        private readonly ILogger<UpdateAssetDescriptionsJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SteamConfiguration _steamConfiguration;

        public UpdateAssetDescriptionsJob(IConfiguration configuration, ILogger<UpdateAssetDescriptionsJob> logger, IServiceScopeFactory scopeFactory)
            : base(logger, configuration.GetJobConfiguration<UpdateAssetDescriptionsJob>())
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

                var assetDescriptions = db.SteamAssetDescriptions
                    .Include(x => x.App)
                    .ToList();

                if (!assetDescriptions.Any())
                {
                    return;
                }

                var language = db.SteamLanguages.FirstOrDefault(x => x.IsDefault);
                if (language == null)
                {
                    return;
                }

                var groupedAssetDescriptions = assetDescriptions.GroupBy(x => x.App);
                foreach (var group in groupedAssetDescriptions)
                {
                    var assetClassIds = group.Select(x => UInt64.Parse(x.SteamId)).ToList();
                    foreach (var batch in assetClassIds.Batch(100)) // Batch to 100 per request to avoid server ban
                    {
                        _logger.LogInformation($"Updating asset description information (ids: {batch.Count()})");
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

                        await Task.WhenAll(
                            assetDescriptionsJoined
                                .Select(x =>
                                    steamService.UpdateAssetDescription(
                                        x.AssetDescription, x.AssetClass
                                    )
                                )
                                .ToArray()
                        );

                        db.SaveChanges();
                    }
                }
            }
        }
    }
}
