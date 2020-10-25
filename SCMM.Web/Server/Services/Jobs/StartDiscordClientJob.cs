using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Discord.Client;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Data.Models.Steam;
using SCMM.Web.Server.Services.Jobs.CronJob;
using System;
using System.Linq;
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

                    // Synchronoise guild list with database
                    var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
                    var discordGuildIds = db.DiscordGuilds
                        .Select(x => x.DiscordId)
                        .ToList();

                    var missingGuilds = discordClient.Guilds
                        .Where(x => !discordGuildIds.Contains(x.Key.ToString()))
                        .ToDictionary(x => x.Key, x => x.Value);
                    if (missingGuilds.Any())
                    {
                        foreach (var missingGuild in missingGuilds)
                        {
                            db.DiscordGuilds.Add(new DiscordGuild()
                            {
                                DiscordId = missingGuild.Key.ToString(),
                                Name = missingGuild.Value
                            });
                        }
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to start the Discord client");
                }
            }
        }
    }
}
