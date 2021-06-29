using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Job.Server.Jobs.Cron;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Steam.Job.Server.Jobs
{
    public class DeleteExpiredImageDataJob : CronJobService
    {
        private readonly ILogger<DeleteExpiredImageDataJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public DeleteExpiredImageDataJob(IConfiguration configuration, ILogger<DeleteExpiredImageDataJob> logger, IServiceScopeFactory scopeFactory)
            : base(logger, configuration.GetJobConfiguration<DeleteExpiredImageDataJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();

            // Delete all images that have expired
            var now = DateTimeOffset.Now;
            var expiredImageData = await db.ImageData
                .Where(x => x.ExpiresOn != null && x.ExpiresOn <= now)
                .OrderByDescending(x => x.ExpiresOn)
                .Take(100) // batch 100 at a time to avoid timing out
                .ToListAsync();

            if (expiredImageData?.Any() == true)
            {
                foreach (var batch in expiredImageData.Batch(10))
                {
                    db.ImageData.RemoveRange(batch);
                    db.SaveChanges();
                }
            }
        }
    }
}
