using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Discord.Client;
using SCMM.Steam.Data.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SCMM.Steam.API.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;

namespace SCMM.Discord.Bot.Server.Middleware
{
    public class DiscordClientMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DiscordClientMiddleware> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly DiscordClient _discordClient;
        private readonly Timer _statusUpdateTimer;

        public DiscordClientMiddleware(RequestDelegate next, ILogger<DiscordClientMiddleware> logger, IServiceScopeFactory scopeFactory, DiscordClient discordClient)
        {
            _next = next;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _statusUpdateTimer = new Timer(OnStatusUpdate);
            _discordClient = discordClient;
            _discordClient.Ready += OnReady;
            _discordClient.GuildJoined += OnGuildJoined;
            _discordClient.GuildLeft += OnGuildLeft;
            _ = _discordClient.ConnectAsync();
        }

        public Task Invoke(HttpContext httpContext)
        {
            return _next(httpContext);
        }

        private void OnReady(IEnumerable<Client.DiscordGuild> guilds)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
                    var discordGuilds = db.DiscordGuilds.Include(x => x.Configurations).ToList();

                    // Start the status update timer
                    _statusUpdateTimer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(1));

                    // Add any guilds that we've joined
                    var joinedGuilds = guilds.Where(x => !discordGuilds.Any(y => y.DiscordId == x.Id.ToString())).ToList();
                    if (joinedGuilds.Any())
                    {
                        foreach (var joined in joinedGuilds)
                        {
                            db.DiscordGuilds.Add(new Steam.Data.Store.DiscordGuild()
                            {
                                DiscordId = joined.Id.ToString(),
                                Name = joined.Name
                            });
                        }
                    }

                    // Update any guilds that we're still a member of
                    var memberGuilds = discordGuilds.Join(guilds,
                            x => x.DiscordId,
                            y => y.Id.ToString(),
                            (x, y) => new
                            {
                                Guild = x,
                                Name = y.Name
                            }
                        )
                        .Where(x => string.IsNullOrEmpty(x.Name))
                        .ToList();
                    if (memberGuilds.Any())
                    {
                        foreach (var member in memberGuilds)
                        {
                            member.Guild.Name = member.Name;
                        }
                    }

                    // Remove any guilds that we've left
                    var leftGuilds = discordGuilds.Where(x => !guilds.Any(y => y.Id == ulong.Parse(x.DiscordId))).ToList();
                    if (leftGuilds.Any())
                    {
                        foreach (var left in leftGuilds)
                        {
                            left.Configurations.Clear();
                            db.DiscordGuilds.Remove(left);
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

        private void OnGuildJoined(Client.DiscordGuild guild)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
                    var discordGuild = db.DiscordGuilds.FirstOrDefault(x => x.DiscordId == guild.Id.ToString());
                    if (discordGuild == null)
                    {
                        db.DiscordGuilds.Add(discordGuild = new Steam.Data.Store.DiscordGuild()
                        {
                            DiscordId = guild.Id.ToString(),
                            Name = guild.Name
                        });
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to synchrnoise guild join (id: {guild.Id}, name: {guild.Name})");
                }
            }
        }

        private void OnGuildLeft(Client.DiscordGuild guild)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
                    var discordGuild = db.DiscordGuilds.FirstOrDefault(x => x.DiscordId == guild.Id.ToString());
                    if (discordGuild != null)
                    {
                        db.DiscordGuilds.Remove(discordGuild);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to synchrnoise guild join (id: {guild.Id}, name: {guild.Name})");
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
                    var storeNextUpdateTime = await queryProcessor.ProcessAsync(new GetStoreNextUpdateTimeRequest());
                    await _discordClient.SetWatchingStatus(
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

    public static class DiscordClientMiddlewareExtensions
    {
        public static IApplicationBuilder UseDiscordClient(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DiscordClientMiddleware>();
        }
    }
}
