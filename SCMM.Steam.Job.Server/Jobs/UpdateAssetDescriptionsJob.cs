using CommandQuery;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Job.Server.Jobs.Cron;

namespace SCMM.Steam.Job.Server.Jobs
{
    public class UpdateAssetDescriptionsJob : CronJobService
    {
        private readonly ILogger<UpdateAssetDescriptionsJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public UpdateAssetDescriptionsJob(IConfiguration configuration, ILogger<UpdateAssetDescriptionsJob> logger, IServiceScopeFactory scopeFactory)
            : base(logger, configuration.GetJobConfiguration<UpdateAssetDescriptionsJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
            var commandProcessor = scope.ServiceProvider.GetService<ICommandProcessor>();

            var cutoff = DateTimeOffset.Now.Subtract(TimeSpan.FromHours(24));
            var assetDescriptions = db.SteamAssetDescriptions
                .Where(x => x.TimeRefreshed == null || x.TimeRefreshed <= cutoff)
                .OrderBy(x => x.TimeRefreshed)
                .Select(x => new
                {
                    AppId = x.App.SteamId,
                    ClassId = x.ClassId
                })
                .Take(30) // batch 30 at a time
                .ToList();

            if (!assetDescriptions.Any())
            {
                return;
            }

            var id = Guid.NewGuid();
            _logger.LogInformation($"Updating asset description information (id: {id}, count: {assetDescriptions.Count()})");
            foreach (var assetDescription in assetDescriptions)
            {
                try
                {
                    await commandProcessor.ProcessWithResultAsync(new ImportSteamAssetDescriptionRequest()
                    {
                        AppId = ulong.Parse(assetDescription.AppId),
                        AssetClassId = assetDescription.ClassId
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to update asset description for '{assetDescription.ClassId}'. {ex.Message}");
                    continue;
                }
            }

            db.SaveChanges();
            _logger.LogInformation($"Updated asset description information (id: {id})");
        }
    }
}
