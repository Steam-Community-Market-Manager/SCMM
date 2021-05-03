using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using System.Threading;
using System.Threading.Tasks;
using SCMM.Steam.Job.Server.Jobs.Cron;
using SCMM.Steam.Job.Server.Jobs;

namespace SCMM.Steam.Job.Server.Jobs
{
    public class RefreshSteamSessionJob : CronJobService
    {
        private readonly ILogger<RecalculateMarketItemSnapshotsJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public RefreshSteamSessionJob(IConfiguration configuration, ILogger<RecalculateMarketItemSnapshotsJob> logger, IServiceScopeFactory scopeFactory)
            : base(logger, configuration.GetJobConfiguration<RecalculateMarketItemSnapshotsJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public override Task DoWork(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var steamSession = scope.ServiceProvider.GetRequiredService<SteamSession>();
            if (steamSession != null)
            {
                steamSession.Refresh(scope.ServiceProvider);
            }

            return Task.CompletedTask;
        }
    }
}
