using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Web.Server.Data;
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
            : base(logger, configuration.GetJobConfiguration<CheckForMissingMarketItemIdsJob>())
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
                _logger.LogInformation($"Checking for missing market item name ids (ids: {itemsWithMissingIds.Count()})");
                var updatedItems = await Observable.Interval(TimeSpan.FromSeconds(30))
                    .Zip(itemsWithMissingIds, (x, y) => y)
                    .Select(async x =>
                        steamService.UpdateMarketItemNameId(x,
                            await commnityClient.GetMarketListingItemNameId(
                                new SteamMarketListingPageRequest()
                                {
                                    AppId = x.App.SteamId,
                                    MarketHashName = x.Description.Name,
                                }
                            )
                        )
                    )
                    .Merge()
                    .Where(x => !String.IsNullOrEmpty(x.SteamId))
                    .ToList();

                if (updatedItems.Any())
                {
                    db.SaveChanges();
                }
            }
        }
    }
}
