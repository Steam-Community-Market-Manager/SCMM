using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Steam.Shared.Requests.Community.Html;
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
    public class ResolveMarketItemIdsJob : CronJobService
    {
        private readonly ILogger<ResolveMarketItemIdsJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public ResolveMarketItemIdsJob(IConfiguration configuration, ILogger<ResolveMarketItemIdsJob> logger, IServiceScopeFactory scopeFactory)
            : base(configuration.GetJobConfiguration<ResolveMarketItemIdsJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();

                var itemsWithMissingIds = db.SteamMarketItems
                    .Include(x => x.App)
                    .Include(x => x.Description)
                    .Where(x => String.IsNullOrEmpty(x.SteamId))
                    .ToList();

                if (!itemsWithMissingIds.Any())
                {
                    return;
                }

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

        public async Task<SteamMarketItem> UpdateSteamItemId(SteamDbContext db, SteamMarketItem item)
        {
            var itemNameId = await new SteamClient().GetMarketListingItemNameId(
                new SteamMarketListingPageRequest()
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
