using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
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

        public async Task BroadcastMessage(string message, string title = null, string description = null, string url = null, string thumbnailUrl = null, string imageUrl = null, System.Drawing.Color? color = null)
        {
            EnsureClientIsReady();

            foreach (var guild in _client.Guilds)
            {
                try
                {
                    Embed embed = null;
                    if (!String.IsNullOrEmpty(title))
                    {
                        embed = new EmbedBuilder()
                            .WithTitle(title)
                            .WithDescription(description)
                            .WithUrl(url)
                            .WithImageUrl(imageUrl)
                            .WithThumbnailUrl(thumbnailUrl)
                            .WithColor((color != null ? (Color)color.Value : Color.Default))
                            .WithFooter(x => x.Text = "https://scmm.app")
                            .WithCurrentTimestamp()
                            .Build();
                    }

                    await guild.DefaultChannel.SendMessageAsync(
                        text: message,
                        embed: embed
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to broadcast message to Discord channel (guild: {guild.Id}, name: {guild.Name}, channel: {guild.DefaultChannel.Name})");
                }
            }
        }
    }
}
