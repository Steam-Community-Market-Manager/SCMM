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
    public class DeleteExpiredFileDataJob : CronJobService
    {
        private readonly ILogger<DeleteExpiredFileDataJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public DeleteExpiredFileDataJob(IConfiguration configuration, ILogger<DeleteExpiredFileDataJob> logger, IServiceScopeFactory scopeFactory)
            : base(logger, configuration.GetJobConfiguration<DeleteExpiredFileDataJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();

            // Delete all files that have expired
            var now = DateTimeOffset.Now;
            var expiredFileData = await db.FileData
                .Where(x => x.ExpiresOn != null && x.ExpiresOn <= now)
                .OrderByDescending(x => x.ExpiresOn)
                .Take(100) // batch 100 at a time to avoid timing out
                .ToListAsync();

            if (expiredFileData?.Any() == true)
            {
                foreach (var batch in expiredFileData.Batch(10))
                {
                    db.FileData.RemoveRange(batch);
                    db.SaveChanges();
                }
            }
        }
    }
}
