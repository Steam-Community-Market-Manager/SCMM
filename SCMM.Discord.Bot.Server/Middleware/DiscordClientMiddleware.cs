using CommandQuery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Discord.Client;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Data.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Discord.Bot.Server.Middleware
{
    public class DiscordClientMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DiscordClientMiddleware> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly DiscordClient _client;
        private readonly Timer _statusUpdateTimer;
        private DateTimeOffset _statusNextStoreUpdate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(1));

        public DiscordClientMiddleware(RequestDelegate next, ILogger<DiscordClientMiddleware> logger, IServiceScopeFactory scopeFactory, DiscordClient discordClient)
        {
            _next = next;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _statusUpdateTimer = new Timer(OnStatusUpdate);
            _client = discordClient;
            _client.Connected += OnConnected;
            _client.Disconnected += OnDisconnected;
            _client.Ready += OnReady;
            _client.GuildJoined += OnGuildJoined;
            _client.GuildLeft += OnGuildLeft;
            _ = _client.ConnectAsync().ContinueWith(x =>
            {
                if (x.IsFaulted && x.Exception != null)
                {
                    _logger.LogError(x.Exception, "Failed to connect to Discord");
                }
            });
        }

        public Task Invoke(HttpContext httpContext)
        {
            return _next(httpContext);
        }

        private void OnConnected()
        {
            // Start the status update timer
            _statusUpdateTimer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        private void OnDisconnected()
        {
            // Stop the status update timer
            _statusUpdateTimer.Change(TimeSpan.Zero, TimeSpan.Zero);
        }

        private void OnReady(IEnumerable<Client.DiscordGuild> guilds)
        {
            // Add any missing guilds to our database
            AddGuildsToDatabaseIfMissing(guilds.ToArray());
        }

        private void OnGuildJoined(Client.DiscordGuild guild)
        {
            // Add new guild to our database
            _logger.LogInformation($"New guild was joined: {guild.Name}");
            AddGuildsToDatabaseIfMissing(guild);
        }

        private void OnGuildLeft(Client.DiscordGuild guild)
        {
            _logger.LogInformation($"Guild was left: {guild.Name}");
        }

        private async void OnStatusUpdate(object state)
        {
            // If the next store update time is in the past by more than 6 hours, requery it to get a new timestamp
            if ((_statusNextStoreUpdate - DateTimeOffset.Now).Add(TimeSpan.FromHours(6)) <= TimeSpan.Zero)
            {
                using var scope = _scopeFactory.CreateScope();
                try
                {
                    var queryProcessor = scope.ServiceProvider.GetRequiredService<IQueryProcessor>();
                    var storeNextUpdateTime = await queryProcessor.ProcessAsync(new GetStoreNextUpdateTimeRequest());
                    _statusNextStoreUpdate = storeNextUpdateTime.Timestamp;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to get the store next update time for client watching status");
                }
            }

            // Display a countdown until the next store update using the last cached timestamp
            var nextStoreIsOverdue = (_statusNextStoreUpdate <= DateTimeOffset.Now);
            var nextStoreTimeRemaining = (_statusNextStoreUpdate - DateTimeOffset.Now);
            var nextStoreTimeDescription = nextStoreTimeRemaining.ToDurationString(
                prefix: (nextStoreIsOverdue ? "overdue by" : "due in"), zero: "due now", showSeconds: false, maxGranularity: 2
            );

            if (_client.IsConnected)
            {
                await _client.SetWatchingStatusAsync($"the store, {nextStoreTimeDescription}");
            }
        }

        private void AddGuildsToDatabaseIfMissing(params Client.DiscordGuild[] guilds)
        {
            using var scope = _scopeFactory.CreateScope();
            try
            {
                var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
                var discordGuildIds = db.DiscordGuilds
                    .Select(x => x.DiscordId)
                    .AsNoTracking()
                    .ToList();

                var missingGuilds = guilds.Where(x => !discordGuildIds.Any(y => y == x.Id.ToString())).ToList();
                if (missingGuilds.Any())
                {
                    foreach (var guild in missingGuilds)
                    {
                        _logger.LogInformation($"New guild was joined: {guild.Name}");
                        db.DiscordGuilds.Add(new Steam.Data.Store.DiscordGuild()
                        {
                            DiscordId = guild.Id.ToString(),
                            Name = guild.Name
                        });
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to add newly joined guilds to persistent storage (count: {guilds.Length})");
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
