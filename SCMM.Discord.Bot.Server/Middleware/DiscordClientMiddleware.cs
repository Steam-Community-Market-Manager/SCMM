using Microsoft.Extensions.Caching.Distributed;
using SCMM.Discord.Client;
using SCMM.Discord.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Web.Client.Extensions;
using System.Text.RegularExpressions;

namespace SCMM.Discord.Bot.Server.Middleware
{
    public class DiscordClientMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DiscordClientMiddleware> _logger;
        private readonly DiscordClient _client;
        private readonly IDistributedCache _cache;

        public DiscordClientMiddleware(RequestDelegate next, ILogger<DiscordClientMiddleware> logger, DiscordClient discordClient, IDistributedCache cache)
        {
            _next = next;
            _logger = logger;
            _cache = cache;
            _client = discordClient;
            _client.Connected += OnConnected;
            _ = _client.ConnectAsync().ContinueWith(x =>
            {
                if (x.IsFaulted && x.Exception != null)
                {
                    _logger.LogError(x.Exception, "Failed to connect to Discord");
                }
            });
            _cache = cache;
        }

        public Task Invoke(HttpContext httpContext)
        {
            return _next(httpContext);
        }

        private void OnConnected()
        {
            _ = RepopulateSystemLatestChangeMessagesCache();
        }

        // TODO: Move this somewhere else and hook in to channel message events so this happens automatically when new messages are sent
        private async Task RepopulateSystemLatestChangeMessagesCache()
        {
            var messages = await _client.GetMessagesAsync(
                guildId: 935704534808920114, // TODO: Move to config
                channelId: 935710112063041546, // TODO: Move to config
                messageLimit: 10
            );

            var latestSystemChanges = messages
                .Where(x => !String.IsNullOrEmpty(x.Content))
                .Select(m => new TextMessage()
                {
                    Id = m.Id,
                    AuthorId = m.AuthorId,
                    // Remove all Discord mentions/channels/users tags from the message content
                    Content = Regex.Replace(m.Content, @"<[@&#]*[0-9]+>", String.Empty, RegexOptions.IgnoreCase).Trim().FirstCharToUpper(),
                    Attachments = m.Attachments?.Select(a => new MessageAttachment()
                    {
                        Id = a.Id,
                        Url = a.Url,
                        FileName = a.FileName,
                        ContentType = a.ContentType,
                        Description = a.Description
                    })?.ToArray(),
                    Timestamp = m.Timestamp
                });

            if (latestSystemChanges.Any())
            {
                await _cache.SetJsonAsync(
                    SCMM.Steam.Data.Models.Constants.LatestSystemUpdatesCacheKey,
                    latestSystemChanges.ToArray()
                );
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
