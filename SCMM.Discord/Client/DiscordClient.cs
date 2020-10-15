using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Discord.Client
{
    public class DiscordClient : IDisposable
    {
        private readonly ILogger<DiscordClient> _logger;
        private readonly DiscordConfiguration _configuration;
        private readonly DiscordSocketClient _client;
        private readonly Task _clientStartTask;
        private readonly ManualResetEvent _clientReady;
        private bool disposedValue;

        public DiscordClient(ILogger<DiscordClient> logger, DiscordConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _client = new DiscordSocketClient();
            _client.Log += LogClientMessage;
            _client.Ready += async () => _clientReady.Set();
            _clientStartTask = LoginAndStartClient();
            _clientReady = new ManualResetEvent(false);
        }

        private Task LogClientMessage(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                    _logger.LogCritical(message.Exception, message.Message);
                    break;
                case LogSeverity.Error:
                    _logger.LogError(message.Exception, message.Message);
                    break;
                case LogSeverity.Warning:
                    _logger.LogWarning(message.Exception, message.Message);
                    break;
                case LogSeverity.Info:
                    _logger.LogInformation(message.Exception, message.Message);
                    break;
            }

            return Task.CompletedTask;
        }

        private async Task LoginAndStartClient()
        {
            await _client.LoginAsync(TokenType.Bot, _configuration.BotToken);
            await _client.StartAsync();
        }

        private void EnsureClientIsReady()
        {
            _clientReady.WaitOne(TimeSpan.FromSeconds(30));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _client.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public Task BroadcastMessage(string message, string title = null, string description = null, IDictionary<string, string> fields = null, string url = null, string thumbnailUrl = null, string imageUrl = null, System.Drawing.Color? color = null)
        {
            return BroadcastMessage(null, null, message, title: title, description: description, fields: fields, url: url, thumbnailUrl: thumbnailUrl, imageUrl: imageUrl, color: color);
        }

        public Task BroadcastMessage(string channelPattern, string message, string title = null, string description = null, IDictionary<string, string> fields = null, string url = null, string thumbnailUrl = null, string imageUrl = null, System.Drawing.Color? color = null)
        {
            return BroadcastMessage(null, channelPattern, message, title: title, description: description, url: url, fields: fields, thumbnailUrl: thumbnailUrl, imageUrl: imageUrl, color: color);
        }

        public async Task BroadcastMessage(string guildPattern, string channelPattern, string message, string title = null, string description = null, IDictionary<string, string> fields = null, string url = null, string thumbnailUrl = null, string imageUrl = null, System.Drawing.Color? color = null)
        {
            EnsureClientIsReady();

            // If guild is null, we'll broadcast to all guilds
            var guilds = _client.Guilds
                .Where(x => String.IsNullOrEmpty(guildPattern) || Regex.IsMatch(x.Name, guildPattern));
            foreach (var guild in guilds)
            {
                // If the channel is null, we'll broadcast to the first channel that we have permission to
                var channels = guild.TextChannels
                    .Where(x => String.IsNullOrEmpty(channelPattern) || Regex.IsMatch(x.Name, channelPattern))
                    .ToList();
                if (!channels.Any() && !String.IsNullOrEmpty(channelPattern))
                {
                    // If the channel pattern didn't match anything, broadcast to the first channel that we havve permission to
                    channels.AddRange(guild.TextChannels);
                }

                foreach (var channel in channels)
                {
                    // Make sure we have permission to send messages here
                    var channelPermissions = guild.CurrentUser.GetPermissions(channel);
                    if (!channelPermissions.SendMessages)
                    {
                        continue;
                    }

                    try
                    {
                        // If the title is not null, we assume you have emdeded content
                        var embed = (Embed)null;
                        if (!String.IsNullOrEmpty(title))
                        {
                            var fieldBuilders = new List<EmbedFieldBuilder>();
                            if (fields != null)
                            {
                                fieldBuilders = fields.Select(x => new EmbedFieldBuilder()
                                    .WithName(x.Key)
                                    .WithValue(x.Value)
                                ).ToList();
                            }

                            embed = new EmbedBuilder()
                                .WithTitle(title)
                                .WithDescription(description)
                                .WithFields(fieldBuilders)
                                .WithUrl(url)
                                .WithImageUrl(imageUrl)
                                .WithThumbnailUrl(thumbnailUrl)
                                .WithColor((color != null ? (Color)color.Value : Color.Default))
                                .WithFooter(x => x.Text = "https://scmm.app")
                                .WithCurrentTimestamp()
                                .Build();
                        }

                        // Send the message
                        await channel.SendMessageAsync(
                            text: message,
                            embed: embed
                        );

                        // Break out of the channel loop.
                        // We only want to send the message to the FIRST channel that matches our critera PER guild
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to broadcast message to Discord (guild: {guild.Name}, channel: {channel.Name})");
                        continue;
                    }
                }
            }
        }
    }
}
