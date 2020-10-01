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

        public Task BroadcastMessage(string channel, string message, string title = null, string description = null, IDictionary<string, string> fields = null, string url = null, string thumbnailUrl = null, string imageUrl = null, System.Drawing.Color? color = null)
        {
            return BroadcastMessage(null, channel, message, title: title, description: description, url: url, fields: fields, thumbnailUrl: thumbnailUrl, imageUrl: imageUrl, color: color);
        }

        public async Task BroadcastMessage(string guild, string channel, string message, string title = null, string description = null, IDictionary<string, string> fields = null, string url = null, string thumbnailUrl = null, string imageUrl = null, System.Drawing.Color? color = null)
        {
            EnsureClientIsReady();

            // If guild is null, we'll broadcast to all guilds
            var guilds = _client.Guilds
                .Where(x => String.IsNullOrEmpty(guild) || Regex.IsMatch(x.Name, guild));
            foreach (var targetGuild in guilds)
            {
                // If the channel is null, we'll broadcast to the default channel
                var targetChannel = targetGuild.TextChannels
                    .FirstOrDefault(x => !String.IsNullOrEmpty(guild) && Regex.IsMatch(x.Name, channel));
                if (targetChannel == null)
                {
                    targetChannel = targetGuild.DefaultChannel;
                }

                try
                {
                    // If the title is not null, we assume you have emdeded content
                    var embed = (Embed) null;
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
                    await targetChannel.SendMessageAsync(
                        text: message,
                        embed: embed
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to broadcast message to Discord (guild: {targetGuild.Name}, channel: {targetChannel.Name})");
                }
            }
        }
    }
}
