using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Services.Jobs.CronJob;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs
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
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ScmmDbContext>();

                var now = DateTimeOffset.Now;
                var expiredImageData = db.ImageData
                    .Where(x => x.ExpiresOn != null && x.ExpiresOn <= now)
                    .OrderByDescending(x => x.ExpiresOn)
                    .Take(100) // to avoid timing out
                    .ToList();

                if (expiredImageData?.Any() == true)
                {
                    db.ImageData.RemoveRange(expiredImageData);
                    db.SaveChanges();
                }
            }
        }
    }
}
