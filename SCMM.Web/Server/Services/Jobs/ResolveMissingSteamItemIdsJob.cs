using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Steam.Shared.Requests;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain.Models.Steam;
using SCMM.Web.Server.Services.Jobs.CronJob;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs
{
    public class ResolveMissingSteamItemIdsJob : CronJobService
    {
        private readonly ILogger<ResolveMissingSteamItemIdsJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public ResolveMissingSteamItemIdsJob(IConfiguration configuration, ILogger<ResolveMissingSteamItemIdsJob> logger, IServiceScopeFactory scopeFactory)
            : base(configuration.GetJobConfiguration<ResolveMissingSteamItemIdsJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
                
                var itemsWithMissingIds = db.SteamItems
                    .Include(x => x.App)
                    .Include(x => x.Description)
                    .Where(x => String.IsNullOrEmpty(x.SteamId))
                    .ToList();

                if (!itemsWithMissingIds.Any())
                {
                    return;
                }

                // TODO: Error handling
                // TODO: Retry logic
                // Add a 30 second delay between requests to avoid "Too Many Requests" error
                var updatedItems = await Observable.Interval(TimeSpan.FromSeconds(30))
                    .Zip(itemsWithMissingIds, (x, y) => y)
                    .Select(x => Observable.FromAsync(() => UpdateSteamItemId(db, x)))
                    .Merge()
                    .Where(x => !String.IsNullOrEmpty(x.SteamId))
                    .ToList();

                if (updatedItems.Any())
                {
                    await db.SaveChangesAsync();
                }
            }
        }

        public async Task<SteamItem> UpdateSteamItemId(SteamDbContext db, SteamItem item)
        {
            var itemNameId = await new SteamClient().GetMarketListingItemNameId(
                new SteamMarketListingRequest()
                {
                    AppId = item.App.SteamId,
                    MarketHashName = item.Description.Name,
                }
            );

            if (!String.IsNullOrEmpty(itemNameId))
            {
                item.SteamId = itemNameId;
                await db.SaveChangesAsync();
            }

            return item;
        }
    }
}
