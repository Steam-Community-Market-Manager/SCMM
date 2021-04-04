using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Discord.Client;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Data.Models.Discord;
using SCMM.Web.Server.Services.Jobs.CronJob;
using SCMM.Web.Server.Services.Queries;
using SCMM.Web.Shared.Domain;
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
        private readonly Timer _statusUpdateTimer;

        public StartDiscordClientJob(IConfiguration configuration, ILogger<StartDiscordClientJob> logger, IServiceScopeFactory scopeFactory, DiscordClient discordClient)
            : base(logger, configuration.GetJobConfiguration<RepopulateCacheJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _discordClient = discordClient;
            _discordClient.Ready += OnReady;
            _discordClient.GuildJoined += OnGuildJoined;
            _discordClient.GuildLeft += OnGuildLeft;
            _statusUpdateTimer = new Timer(OnStatusUpdate);
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

        private async void OnReady(IDictionary<ulong, string> guilds)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<ScmmDbContext>();
                    var discordGuilds = db.DiscordGuilds.Include(x => x.Configurations).ToList();

                    // Start the status update timer
                    _statusUpdateTimer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(1));

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
                            }
                        )
                        .Where(x => String.IsNullOrEmpty(x.Name))
                        .ToList();
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
                        foreach (var left in leftGuilds)
                        {
                            left.Configurations.Clear();
                            left.BadgeDefinitions.Clear();
                            db.DiscordGuilds.Remove(left);
                        }
                    }

                    // Synchronise VIP roles for users belonging to VIP servers
                    var vipServers = discordGuilds.Where(x => x.Flags.HasFlag(Shared.Data.Models.Discord.DiscordGuildFlags.VIP)).ToList();
                    foreach (var vipServer in vipServers)
                    {
                        try
                        {
                            var vipUsers = await _discordClient.GetUsersWithRoleAsync(ulong.Parse(vipServer.DiscordId), Roles.Donator);
                            if (vipUsers != null)
                            {
                                var newVipUsers = db.SteamProfiles
                                    .Where(x => !x.Roles.Serialised.Contains(Roles.VIP))
                                    .Where(x => vipUsers.Contains(x.DiscordId))
                                    .ToList();

                                foreach (var user in newVipUsers)
                                {
                                    user.Roles.Add(Roles.VIP);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to check guild for new VIP members  (id: {vipServer.DiscordId}, name: {vipServer.Name})");
                            continue;
                        }
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
                    var db = scope.ServiceProvider.GetRequiredService<ScmmDbContext>();
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
                    var db = scope.ServiceProvider.GetRequiredService<ScmmDbContext>();
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

        private async void OnStatusUpdate(object state)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                try
                {
                    var queryProcessor = scope.ServiceProvider.GetRequiredService<IQueryProcessor>();
                    var storeNextUpdateTime= await queryProcessor.ProcessAsync(new GetStoreNextUpdateTimeRequest());
                    await _discordClient.SetStatus(
                        $"the store, {storeNextUpdateTime.TimeDescription}"
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to update discord client status");
                }
            }
        }
    }
}
