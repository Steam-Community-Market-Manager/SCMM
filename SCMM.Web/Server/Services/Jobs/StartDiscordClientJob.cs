using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Discord.Client;
using SCMM.Web.Server.Services.Jobs.CronJob;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs
{
    public class StartDiscordClientJob : CronJobService
    {
        private readonly ILogger<StartDiscordClientJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public StartDiscordClientJob(IConfiguration configuration, ILogger<StartDiscordClientJob> logger, IServiceScopeFactory scopeFactory)
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
                    var discordClient = scope.ServiceProvider.GetRequiredService<DiscordClient>();
                    await discordClient.ConnectAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to start the Discord client");
                }
            }
        }
    }
}
