using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Discord.Client;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain;
using SCMM.Web.Server.Services.Jobs.CronJob;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs
{
    public class CheckForNewAcceptedWorkshopItemsJob : CronJobService
    {
        private readonly ILogger<CheckForNewAcceptedWorkshopItemsJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly DiscordClient _discordClient;

        public CheckForNewAcceptedWorkshopItemsJob(IConfiguration configuration, ILogger<CheckForNewAcceptedWorkshopItemsJob> logger, IServiceScopeFactory scopeFactory, DiscordClient discordClient)
            : base(logger, configuration.GetJobConfiguration<CheckForNewAcceptedWorkshopItemsJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _discordClient = discordClient;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
                var steamService = scope.ServiceProvider.GetRequiredService<SteamService>();

                var steamApps = await db.SteamApps.ToListAsync();
                if (!steamApps.Any())
                {
                    return;
                }

                foreach (var app in steamApps)
                {
                    _logger.LogInformation($"Checking for new accepted workshop items (appId: {app.SteamId})");

                    // ....

                    await db.SaveChangesAsync();
                }
            }
        }

    }
}
