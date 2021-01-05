using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Steam.Shared.Community.Requests.Json;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Services.Jobs.CronJob;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs
{
    public class RepairMissingAppFiltersJob : CronJobService
    {
        private readonly ILogger<RepairMissingAppFiltersJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public RepairMissingAppFiltersJob(IConfiguration configuration, ILogger<RepairMissingAppFiltersJob> logger, IServiceScopeFactory scopeFactory)
            : base(logger, configuration.GetJobConfiguration<RepairMissingAppFiltersJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var commnityClient = scope.ServiceProvider.GetService<SteamCommunityClient>();
                var steamService = scope.ServiceProvider.GetRequiredService<SteamService>();
                var db = scope.ServiceProvider.GetRequiredService<ScmmDbContext>();

                var appsWithMissingFilters = db.SteamApps
                    .Where(x => x.Filters.Count == 0)
                    .Include(x => x.Filters)
                    .ToList();

                foreach (var app in appsWithMissingFilters)
                {
                    var request = new SteamMarketAppFiltersJsonRequest()
                    {
                        AppId = app.SteamId
                    };

                    _logger.LogInformation($"Checking for missing app filters (appId: {app.SteamId})");
                    var response = await commnityClient.GetMarketAppFilters(request);
                    if (response?.Success != true)
                    {
                        _logger.LogError("Failed to get app filters");
                        continue;
                    }

                    var appFilters = response.Facets.Where(x => x.Value?.AppId == app.SteamId).Select(x => x.Value);
                    foreach (var appFilter in appFilters)
                    {
                        steamService.AddOrUpdateAppAssetFilter(app, appFilter);
                    }
                }

                db.SaveChanges();
            }
        }
    }
}
