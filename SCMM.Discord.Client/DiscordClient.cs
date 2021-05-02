using Discord;
using Discord.Commands;
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
        private readonly DiscordShardedClient _client;
        private readonly ManualResetEvent _clientIsConnected;
        private readonly CommandService _commands;
        private readonly DiscordCommandHandler _commandHandler;
        private bool disposedValue;

        public DiscordClient(ILogger<DiscordClient> logger, DiscordConfiguration configuration, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _configuration = configuration;
            _client = new DiscordShardedClient(new DiscordSocketConfig()
            {
                ShardId = configuration.ShardId,
                TotalShards = configuration.TotalShards
            });
            _client.Log += OnClientLogAsync;
            _client.ShardConnected += OnShardConnectedAsync;
            _client.ShardDisconnected += OnShardDisconnectedAsync;
            _client.ShardReady += OnShardReadyAsync;
            _client.JoinedGuild += OnJoinedGuildSayHello;
            _client.JoinedGuild += (x) => Task.Run(() => GuildJoined?.Invoke(new DiscordGuild(x)));
            _client.LeftGuild += (x) => Task.Run(() => GuildLeft?.Invoke(new DiscordGuild(x)));
            _clientIsConnected = new ManualResetEvent(false);
            _commands = new CommandService();
            _commandHandler = new DiscordCommandHandler(logger, serviceProvider, _commands, _client, configuration);
        }

        private Task OnClientLogAsync(LogMessage message)
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
                case LogSeverity.Verbose:
                    _logger.LogTrace(message.Exception, message.Message);
                    break;
                case LogSeverity.Debug:
                    _logger.LogDebug(message.Exception, message.Message);
                    break;
            }

            return Task.CompletedTask;
        }

        private Task OnShardConnectedAsync(DiscordSocketClient shard)
        {
            if (IsConnected)
            {
                _clientIsConnected.Set();
            }

            return Task.CompletedTask;
        }

        private Task OnShardDisconnectedAsync(Exception ex, DiscordSocketClient shard)
        {
            if (!IsConnected)
            {
                _clientIsConnected.Reset();
            }

            return Task.CompletedTask;
        }

        private Task OnShardReadyAsync(DiscordSocketClient shard)
        {
            Ready?.Invoke(shard.Guilds.Select(x => new DiscordGuild(x)));
            return Task.CompletedTask;
        }

        private Task OnJoinedGuildSayHello(SocketGuild guild)
        {
            return guild.DefaultChannel.SendMessageAsync(
                $"Hello! Thanks for adding me. To learn more about my commands, type `{_configuration.CommandPrefix}help`."
            );
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _client.Dispose();
                    _clientIsConnected.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async Task ConnectAsync()
        {
            await _client.LoginAsync(TokenType.Bot, _configuration.BotToken);
            await _client.StartAsync();

            await _commandHandler.AddCommandsAsync();
        }

        public async Task DisconnectAsync()
        {
            await _client.LogoutAsync();
            await _client.StopAsync();
        }

        private void WaitUntilClientIsConnected()
        {
            // For for the singal or 60 seconds, which ever happens first
            _clientIsConnected.WaitOne(TimeSpan.FromSeconds(60));
        }

        public Task SetWatchingStatusAsync(string status)
        {
            WaitUntilClientIsConnected();

            return _client.SetGameAsync(
                status, null, ActivityType.Watching
            );
        }

        public async Task BroadcastMessageAsync(string guildPattern, string channelPattern, string message, string title = null, string description = null, IDictionary<string, string> fields = null, bool fieldsInline = false, string url = null, string thumbnailUrl = null, string imageUrl = null, System.Drawing.Color? color = null)
        {
            WaitUntilClientIsConnected();

            // Pre-build the embed content (so we can share it across all messages)
            // If the title is not null, we assume the message has emdeded content
            var embed = (Embed) null;
            if (!String.IsNullOrEmpty(title))
            {
                var fieldBuilders = new List<EmbedFieldBuilder>();
                if (fields != null)
                {
                    fieldBuilders = fields.Select(x => new EmbedFieldBuilder()
                        .WithName(x.Key)
                        .WithValue(x.Value)
                        .WithIsInline(fieldsInline)
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
                    .Build();
            }

            // Find the guilds that match our pattern
            var guilds = _client.Guilds
                .OrderBy(x => x.Name)
                .Where(x => String.Equals(x.Id.ToString(), guildPattern, StringComparison.InvariantCultureIgnoreCase) || Regex.IsMatch(x.Name, guildPattern))
                .ToList();

            if (!guilds.Any())
            {
                _logger.LogWarning($"Unable to find any suitable guilds to message (guild pattern: {guildPattern})");
                return;
            }

            foreach (var guild in guilds)
            {
                // Find best channel that match our pattern (and that we have permission to post in)
                // NOTE: We only want to send one message per guild
                var channel = guild.TextChannels
                    .OrderBy(x => x.Name)
                    .Where(x => String.Equals($"<#{x.Id}>", channelPattern, StringComparison.InvariantCultureIgnoreCase) || Regex.IsMatch(x.Name, channelPattern))
                    .Where(x => guild.CurrentUser.GetPermissions(x).SendMessages)
                    .FirstOrDefault();

                if (channel == null)
                {
                    _logger.LogWarning($"Unable to find any suitable channels to message (guild: {guild.Name}, channel pattern: {channelPattern})");
                    continue;
                }

                // Send the message
                _logger.LogInformation($"Sending messsage \"{message ?? title}\" (guild: {guild.Name}, channel: {channel.Name})");
                await channel.SendMessageAsync(
                    text: message,
                    embed: embed
                );
            }
        }

        public IEnumerable<DiscordShard> Shards => _client.Shards.Select(x => new DiscordShard(x));

        public IEnumerable<DiscordGuild> Guilds => _client.Guilds.Select(x => new DiscordGuild(x));

        public DiscordUser User => new DiscordUser(_client.CurrentUser);

        public bool IsConnected => _client.Shards.All(x => x.ConnectionState == ConnectionState.Connected && x.LoginState == LoginState.LoggedIn);

        // Events
        public event Action<IEnumerable<DiscordGuild>> Ready;
        public event Action<DiscordGuild> GuildJoined;
        public event Action<DiscordGuild> GuildLeft;
    }
}
