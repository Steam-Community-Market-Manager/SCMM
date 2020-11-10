﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Services.Jobs.CronJob;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs
{
    public class RecalculateMarketItemSnapshotsJob : CronJobService
    {
        private readonly ILogger<RecalculateMarketItemSnapshotsJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public RecalculateMarketItemSnapshotsJob(IConfiguration configuration, ILogger<RecalculateMarketItemSnapshotsJob> logger, IServiceScopeFactory scopeFactory)
            : base(logger, configuration.GetJobConfiguration<RecalculateMarketItemSnapshotsJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var steamService = scope.ServiceProvider.GetRequiredService<SteamService>();
                var db = scope.ServiceProvider.GetRequiredService<ScmmDbContext>();

                var itemIds = await db.SteamMarketItems
                    .Select(x => x.Id)
                    .ToListAsync();

                foreach (var itemId in itemIds)
                {
                    var item = await db.SteamMarketItems
                        .Include(x => x.BuyOrders)
                        .Include(x => x.SellOrders)
                        .Include(x => x.SalesHistory)
                        .SingleOrDefaultAsync(x => x.Id == itemId);

                    item.RecalculateOrders();
                    item.RecalculateSales();
                    db.SaveChanges();
                }
            }
        }
    }
}
