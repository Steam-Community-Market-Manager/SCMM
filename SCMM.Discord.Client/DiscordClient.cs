using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using SCMM.Discord.Client.Extensions;
using SCMM.Shared.Client;
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
                Connected?.Invoke();
            }

            return Task.CompletedTask;
        }

        private Task OnShardDisconnectedAsync(Exception ex, DiscordSocketClient shard)
        {
            if (!IsConnected)
            {
                _clientIsConnected.Reset();
                Disconnected?.Invoke();
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

        public IDisposable SubscribeToReplies(ulong messageId, Func<string, Task> onReply)
        {
            var replyCallback = (Func<SocketMessage, Task>)(
                (msg) =>
                {
                    if (msg.Reference != null && msg.Reference.MessageId.IsSpecified && msg.Reference.MessageId.Value == messageId)
                    {
                        return onReply(msg?.Content);
                    }
                    return Task.CompletedTask;
                }
            );

            _client.MessageReceived += replyCallback;
            return new DisposableDelegate(
                () => _client.MessageReceived -= replyCallback
            );
        }

        public IDisposable SubscribeToReactions(ulong messageId, Func<string, Task> onReaction)
        {
            var reactionCallback = (Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task>)(
                (msg, channel, reaction) =>
                {
                    return onReaction(reaction?.Emote?.Name);
                }
            );

            _client.ReactionAdded += reactionCallback;
            return new DisposableDelegate(
                () => _client.ReactionAdded -= reactionCallback
            );
        }

        public async Task<ulong> SendMessageAsync(string username, string message, string title = null, string description = null, IDictionary<string, string> fields = null, bool fieldsInline = false, string url = null, string thumbnailUrl = null, string imageUrl = null, System.Drawing.Color? color = null, string[] reactions = null)
        {
            WaitUntilClientIsConnected();

            // Find the user
            var usernameParts = username.Split("#", StringSplitOptions.TrimEntries);
            var user = _client.GetUser(usernameParts.FirstOrDefault(), usernameParts.LastOrDefault());
            if (user == null)
            {
                throw new Exception($"Unable to find user for message \"{message ?? title}\" (username: {username})");
            }

            // Create a DM channel with the user
            var dm = await user.GetOrCreateDMChannelAsync();
            if (dm == null)
            {
                throw new Exception($"Unable to create DM channel for message \"{message ?? title}\" (username: {username})");
            }

            // Send the message
            var embed = BuildEmbed(title, description, fields, fieldsInline, url, thumbnailUrl, imageUrl, color);
            var msg = await dm.SendMessageAsync(text: message, embed: embed);
            if (msg == null)
            {
                throw new Exception($"Unable to send message \"{message ?? title}\" (username: {username})");
            }

            if (reactions?.Any() == true)
            {
                foreach (var reaction in reactions)
                {
                    await msg.AddReactionSafeAsync(new Emoji(reaction));
                }
            }

            return msg.Id;
        }

        public async Task<ulong> SendMessageAsync(ulong guildId, string channelPattern, string message, string title = null, string description = null, IDictionary<string, string> fields = null, bool fieldsInline = false, string url = null, string thumbnailUrl = null, string imageUrl = null, System.Drawing.Color? color = null)
        {
            WaitUntilClientIsConnected();

            // Find the guilds that match our pattern
            var guild = _client.Guilds.FirstOrDefault(x => x.Id == guildId);
            if (guild == null)
            {
                throw new Exception($"Unable to find guild (id: {guildId})");
            }

            // Find best channel that match our pattern (and that we have permission to post in)
            // NOTE: We only want to send one message per guild
            var channel = guild.TextChannels
                .OrderBy(x => x.Name)
                .Where(x => String.Equals($"<#{x.Id}>", channelPattern, StringComparison.InvariantCultureIgnoreCase) || Regex.IsMatch(x.Name, channelPattern))
                .Where(x => guild.CurrentUser.GetPermissions(x).SendMessages)
                .FirstOrDefault();

            if (channel == null)
            {
                _logger.LogWarning($"Unable to find any suitable channels to post message (guild: {guild.Name} #{guild.Id}, channel pattern: {channelPattern})");
            }

            // Send the message
            _logger.LogInformation($"Sending messsage \"{message ?? title}\" (guild: {guild.Name} #{guild.Id}, channel: {channel.Name})");
            var embed = BuildEmbed(title, description, fields, fieldsInline, url, thumbnailUrl, imageUrl, color);
            var msg = await channel.SendMessageAsync(text: message, embed: embed);
            if (msg == null)
            {
                throw new Exception($"Unable to send message \"{message ?? title}\" (guild: {guild.Name} #{guild.Id}, channel: {channel.Name})");
            }

            return msg.Id;
        }

        private Embed BuildEmbed(string title = null, string description = null, IDictionary<string, string> fields = null, bool fieldsInline = false, string url = null, string thumbnailUrl = null, string imageUrl = null, System.Drawing.Color? color = null)
        {
            // Pre-build the embed content (so we can share it across all messages)
            // If the title is not null, we assume the message has emdeded content
            if (!String.IsNullOrEmpty(title))
            {
                var fieldBuilders = new List<EmbedFieldBuilder>();
                if (fields != null)
                {
                    fieldBuilders = fields.Select(x => new EmbedFieldBuilder()
                        .WithName(x.Key)
                        .WithValue(String.IsNullOrEmpty(x.Value) ? "-" : x.Value)
                        .WithIsInline(fieldsInline)
                    ).ToList();
                }

                return new EmbedBuilder()
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

            return null;
        }

        public IEnumerable<DiscordShard> Shards => _client.Shards.Select(x => new DiscordShard(x));

        public IEnumerable<DiscordGuild> Guilds => _client.Guilds.Select(x => new DiscordGuild(x));

        public DiscordUser User => new DiscordUser(_client.CurrentUser);

        public bool IsConnected => _client.Shards.All(x => x.ConnectionState == ConnectionState.Connected && x.LoginState == LoginState.LoggedIn);

        // Events
        public event Action Connected;
        public event Action Disconnected;
        public event Action<IEnumerable<DiscordGuild>> Ready;
        public event Action<DiscordGuild> GuildJoined;
        public event Action<DiscordGuild> GuildLeft;
    }
}
