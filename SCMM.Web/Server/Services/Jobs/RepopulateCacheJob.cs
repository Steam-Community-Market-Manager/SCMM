using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Web.Server.Domain;
using SCMM.Web.Server.Services.Jobs.CronJob;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs
{
    public class RepopulateCacheJob : CronJobService
    {
        private readonly ILogger<RepopulateCacheJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public RepopulateCacheJob(IConfiguration configuration, ILogger<RepopulateCacheJob> logger, IServiceScopeFactory scopeFactory)
            : base(logger, configuration.GetJobConfiguration<RepopulateCacheJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                try
                {
                    var languageService = scope.ServiceProvider.GetRequiredService<SteamLanguageService>();
                    await languageService.RepopulateCache();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to repopulate the language cache");
                }
                try
                {
                    var currencyService = scope.ServiceProvider.GetRequiredService<SteamCurrencyService>();
                    await currencyService.RepopulateCache();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to repopulate the currency cache");
                }
            }
        }
    }
}
