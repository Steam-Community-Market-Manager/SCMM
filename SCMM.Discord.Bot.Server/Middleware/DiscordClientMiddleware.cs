using CommandQuery;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Client;
using SCMM.Discord.Data.Store;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.API.Queries;
using DiscordConfiguration = SCMM.Discord.Client.DiscordConfiguration;

namespace SCMM.Discord.Bot.Server.Middleware
{
    public class DiscordClientMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DiscordClientMiddleware> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IDbContextFactory<DiscordDbContext> _discordDbFactory;
        private readonly DiscordClient _client;
        private readonly DiscordConfiguration _configuration;
        private readonly Timer _statusUpdateTimer;
        private DateTimeOffset _statusNextStoreUpdate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(1));

        public DiscordClientMiddleware(RequestDelegate next, ILogger<DiscordClientMiddleware> logger, IServiceScopeFactory serviceScopeFactory, IDbContextFactory<DiscordDbContext> discordDbFactory, DiscordConfiguration discordConfiguration, DiscordClient discordClient)
        {
            _next = next;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _discordDbFactory = discordDbFactory;
            _statusUpdateTimer = new Timer(OnStatusUpdate);
            _configuration = discordConfiguration;
            _client = discordClient;
            _client.Connected += OnConnected;
            _client.Disconnected += OnDisconnected;
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
            _ = Task.Run(async () =>
            {
                var currentGuilds = _client.Guilds.ToArray();
                await AddGuildsToDatabaseIfMissing(currentGuilds);
                await RemoveGuildsFromDatabaseIfOrphaned(currentGuilds);
            });
        }

        private void OnDisconnected()
        {
            // Stop the status update timer
            _statusUpdateTimer.Change(TimeSpan.Zero, TimeSpan.Zero);
        }

        private void OnGuildJoined(Client.DiscordGuild guild)
        {
            // Add new guild to our database
            _logger.LogInformation($"Guild joined: {guild.Name} #{guild.Id}");
            _ = AddGuildsToDatabaseIfMissing(guild);
        }

        private async void OnGuildLeft(Client.DiscordGuild guild)
        {
            _logger.LogInformation($"Guild left: {guild.Name} #{guild.Id}");
            _ = RemoveGuildsFromDatabaseIfExisting(guild.Id);
        }

        private async void OnStatusUpdate(object state)
        {
            // If the next store update time is in the past by more than 6 hours, requery it to get a new timestamp
            if ((_statusNextStoreUpdate - DateTimeOffset.Now).Add(TimeSpan.FromHours(6)) <= TimeSpan.Zero)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                try
                {
                    var queryProcessor = scope.ServiceProvider.GetRequiredService<IQueryProcessor>();
                    var storeNextUpdateTime = await queryProcessor.ProcessAsync(new GetStoreNextUpdateTimeRequest()
                    {
                        AppId = _configuration.AppId
                    });
                    if (storeNextUpdateTime == null)
                    {
                        return; // No stores to report on...
                    }
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
            var nextStoreTimeDescription = nextStoreTimeRemaining.Duration().ToDurationString(
                prefix: (nextStoreIsOverdue ? "overdue by" : "due in"), zero: "due now", showSeconds: false, maxGranularity: 2
            );

            if (_client.IsConnected)
            {
                await _client.SetWatchingStatusAsync($"the store, {nextStoreTimeDescription}");
            }
        }

        private async Task AddGuildsToDatabaseIfMissing(params Client.DiscordGuild[] guilds)
        {
            using var db = await _discordDbFactory.CreateDbContextAsync();
            try
            {
                var discordGuildIds = await db.DiscordGuilds
                    .Select(x => x.Id)
                    .ToListAsync();

                var missingGuilds = guilds.Where(x => !discordGuildIds.Any(y => y == x.Id)).ToList();
                if (missingGuilds.Any())
                {
                    foreach (var guild in missingGuilds)
                    {
                        _logger.LogInformation($"New guild was joined: {guild.Name} #{guild.Id}");
                        db.DiscordGuilds.Add(new Discord.Data.Store.DiscordGuild()
                        {
                            Id = guild.Id,
                            Name = guild.Name
                        });
                    }

                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to add guilds to persistent storage (count: {guilds.Length})");
            }
        }

        private async Task RemoveGuildsFromDatabaseIfOrphaned(params Client.DiscordGuild[] guilds)
        {
            using var db = await _discordDbFactory.CreateDbContextAsync();
            try
            {
                var knownGuildIds = await db.DiscordGuilds
                   .Select(x => x.Id)
                   .ToListAsync();
                var orphanedGuildIds = knownGuildIds
                    .Except(guilds.Select(x => x.Id))
                    .ToArray();
                if (orphanedGuildIds.Any())
                {
                    await RemoveGuildsFromDatabaseIfExisting(orphanedGuildIds);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to remove orphaned guilds from persistent storage");
            }
        }

        private async Task RemoveGuildsFromDatabaseIfExisting(params ulong[] guildIds)
        {
            using var db = await _discordDbFactory.CreateDbContextAsync();
            try
            {
                var guildsToRemove = await db.DiscordGuilds
                    .Where(x => guildIds.Contains(x.Id))
                    .ToListAsync();

                if (guildsToRemove.Any())
                {
                    db.DiscordGuilds.RemoveRange(guildsToRemove);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to remove guilds from persistent storage (count: {guildIds.Length})");
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
