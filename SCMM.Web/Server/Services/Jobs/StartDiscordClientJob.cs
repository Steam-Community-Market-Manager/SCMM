using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Discord.Client;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Data.Models.Steam;
using SCMM.Web.Server.Services.Jobs.CronJob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs
{
    public class StartDiscordClientJob : CronJobService
    {
        private readonly ILogger<StartDiscordClientJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly DiscordClient _discordClient;

        public StartDiscordClientJob(IConfiguration configuration, ILogger<StartDiscordClientJob> logger, IServiceScopeFactory scopeFactory, DiscordClient discordClient)
            : base(logger, configuration.GetJobConfiguration<RepopulateCacheJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _discordClient = discordClient;
            _discordClient.Ready += OnReady;
            _discordClient.GuildJoined += OnGuildJoined;
            _discordClient.GuildLeft += OnGuildLeft;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            try
            {
                await _discordClient.ConnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start the Discord client");
            }
        }

        private void OnReady(IDictionary<ulong, string> guilds)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
                    var discordGuilds = db.DiscordGuilds.ToList();

                    // Add any guilds that we've joined
                    var joinedGuilds = guilds.Where(x => !discordGuilds.Any(y => y.DiscordId == x.Key.ToString())).ToList();
                    if (joinedGuilds.Any())
                    {
                        foreach (var joined in joinedGuilds)
                        {
                            db.DiscordGuilds.Add(new DiscordGuild()
                            {
                                DiscordId = joined.Key.ToString(),
                                Name = joined.Value
                            });
                        }
                    }

                    // Update any guilds that we're still a member of
                    var memberGuilds = discordGuilds.Join(guilds,
                        x => x.DiscordId,
                        y => y.Key.ToString(),
                        (x, y) => new
                        {
                            Guild = x,
                            Name = y.Value
                        });
                    if (memberGuilds.Any())
                    {
                        foreach (var member in memberGuilds)
                        {
                            member.Guild.Name = member.Name;
                        }
                    }

                    // Remove any guilds that we've left
                    var leftGuilds = discordGuilds.Where(x => !guilds.ContainsKey(UInt64.Parse(x.DiscordId))).ToList();
                    if (leftGuilds.Any())
                    {
                        db.DiscordGuilds.RemoveRange(leftGuilds);
                    }

                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to synchronoise list of Discord guilds");
                }
            }
        }

        private void OnGuildJoined(ulong id, string name)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
                    var discordGuild = db.DiscordGuilds.FirstOrDefault(x => x.DiscordId == id.ToString());
                    if (discordGuild == null)
                    {
                        db.DiscordGuilds.Add(discordGuild = new DiscordGuild()
                        {
                            DiscordId = id.ToString(),
                            Name = name
                        });
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to synchrnoise guild join (id: {id}, name: {name})");
                }
            }
        }

        private void OnGuildLeft(ulong id, string name)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
                    var discordGuild = db.DiscordGuilds.FirstOrDefault(x => x.DiscordId == id.ToString());
                    if (discordGuild != null)
                    {
                        db.DiscordGuilds.Remove(discordGuild);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to synchrnoise guild join (id: {id}, name: {name})");
                }
            }
        }
    }
}
