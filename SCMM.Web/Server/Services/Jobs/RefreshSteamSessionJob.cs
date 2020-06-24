using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Web.Server.Services.Jobs.CronJob;
using System.Threading;
using System.Threading.Tasks;
using SCMM.Steam.Client;

namespace SCMM.Web.Server.Services.Jobs
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

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var steamSession = scope.ServiceProvider.GetRequiredService<SteamSession>();
                if (steamSession != null)
                {
                    steamSession.Refresh(scope.ServiceProvider);
                }
            }
        }
    }
}
