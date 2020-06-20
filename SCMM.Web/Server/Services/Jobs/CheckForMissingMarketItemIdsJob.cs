using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain;
using SCMM.Web.Server.Services.Jobs.CronJob;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs
{
    public class CheckForMissingMarketItemIdsJob : CronJobService
    {
        private readonly ILogger<CheckForMissingMarketItemIdsJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public CheckForMissingMarketItemIdsJob(IConfiguration configuration, ILogger<CheckForMissingMarketItemIdsJob> logger, IServiceScopeFactory scopeFactory)
            : base(configuration.GetJobConfiguration<CheckForMissingMarketItemIdsJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var steamService = scope.ServiceProvider.GetRequiredService<SteamService>(); 
                var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();

                var itemsWithMissingIds = db.SteamMarketItems
                    .Where(x => String.IsNullOrEmpty(x.SteamId))
                    .Include(x => x.App)
                    .Include(x => x.Description)
                    .Take(10) // batch 10 at a time
                    .ToList();

                if (!itemsWithMissingIds.Any())
                {
                    return;
                }

                // Add a 30 second delay between requests to avoid "Too Many Requests" error
                var updatedItems = await Observable.Interval(TimeSpan.FromSeconds(30))
                    .Zip(itemsWithMissingIds, (x, y) => y)
                    .Select(x => Observable.FromAsync(() => steamService.UpdateSteamItemId(x)))
                    .Merge()
                    .Where(x => !String.IsNullOrEmpty(x.SteamId))
                    .ToList();

                if (updatedItems.Any())
                {
                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
