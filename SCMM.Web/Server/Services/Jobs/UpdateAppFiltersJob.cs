using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Steam.Shared.Community.Requests.Json;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain;
using SCMM.Web.Server.Services.Jobs.CronJob;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs
{
    public class UpdateAppFiltersJob : CronJobService
    {
        private readonly ILogger<UpdateAppFiltersJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public UpdateAppFiltersJob(IConfiguration configuration, ILogger<UpdateAppFiltersJob> logger, IServiceScopeFactory scopeFactory)
            : base(configuration.GetJobConfiguration<UpdateAppFiltersJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var commnityClient = new SteamCommunityClient();
                var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
                var apps = db.SteamApps.Include(x => x.Filters).ToList();
                foreach (var app in apps)
                {
                    var request = new SteamMarketAppFiltersJsonRequest()
                    {
                        AppId = app.SteamId
                    };

                    var response = await commnityClient.GetMarketAppFilters(request);
                    if (response?.Success != true)
                    {
                        // TODO: Log this...
                        continue;
                    }

                    var appFilters = response.Facets.Where(x => x.Value?.AppId == app.SteamId).Select(x => x.Value);
                    foreach (var appFilter in appFilters)
                    {
                        await SteamService.AddOrUpdateAppAssetFilter(db, app, appFilter);
                    }
                }

                await db.SaveChangesAsync();
            }
        }
    }
}
