using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using SCMM.Discord.Client.Extensions;
using SCMM.Shared.Client;
using System.Text.RegularExpressions;

namespace SCMM.Discord.Client
{
    public class DiscordClient : IDisposable
    {
        private readonly ILogger<DiscordClient> _logger;
        private readonly DiscordConfiguration _configuration;
        private readonly DiscordShardedClient _client;
        private readonly ManualResetEvent _clientIsConnected;
        private readonly DiscordCommandHandler _commandHandler;
        private readonly DiscordInteractionHandler _interactionHandler;
        private bool handlersRegistered;
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
            _commandHandler = new DiscordCommandHandler(logger, serviceProvider, _client, configuration);
            _interactionHandler = new DiscordInteractionHandler(logger, serviceProvider, _client, configuration);
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

        private async Task OnShardConnectedAsync(DiscordSocketClient shard)
        {
            if (IsConnected)
            {
                if (!handlersRegistered)
                {
                    lock(this)
                    {
                        handlersRegistered = true;
                        _ = _commandHandler.AddCommandsAsync();
                        _ = _interactionHandler.AddInteractionsAsync();
                    }
                }

                _clientIsConnected.Set();
                Connected?.Invoke();
            }
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

        private async Task OnJoinedGuildSayHello(SocketGuild guild)
        {
            if (guild.CurrentUser.GetPermissions(guild.DefaultChannel).SendMessages)
            {
                await guild.DefaultChannel.SendMessageAsync(
                    $"Hello! Thanks for adding me. Type `/` to learn more about my commands."
                );
            }
            else
            {
                _logger.LogWarning($"Unable to send welcome message after joining new guild due to insufficent permissions (guild: {guild.Name} #{guild.Id})");
            }
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

        public async Task<ulong> SendMessageAsync(string username, string message, string title = null, string description = null, IDictionary<string, string> fields = null, bool fieldsInline = false, string url = null, string thumbnailUrl = null, string imageUrl = null, Color? color = null, string[] reactions = null)
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
            var dm = await user.CreateDMChannelAsync();
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
                    await msg.TryAddReactionAsync(new Emoji(reaction));
                }
            }

            return msg.Id;
        }

        public async Task<ulong> SendMessageAsync(ulong guildId, string[] channelPatterns, string message, string title = null, string description = null, IDictionary<string, string> fields = null, bool fieldsInline = false, string url = null, string thumbnailUrl = null, string imageUrl = null, Color? color = null)
        {
            WaitUntilClientIsConnected();

            // Find the guilds that match our pattern
            var guild = _client.GetGuild(guildId);
            if (guild == null)
            {
                throw new Exception($"Unable to find guild (id: {guildId})");
            }

            // Find channels that match our pattern (and that we have permission to post in)
            channelPatterns = channelPatterns?.Where(x => !String.IsNullOrEmpty(x))?.ToArray();
            foreach (var channelPattern in channelPatterns)
            {
                var channel = guild.TextChannels
                    .OrderBy(x => x.Name)
                    .Where(x => guild.CurrentUser.GetPermissions(x).SendMessages)
                    .Where(x => string.Equals($"<#{x.Id}>", channelPattern, StringComparison.InvariantCultureIgnoreCase) || Regex.IsMatch(x.Name, channelPattern))
                    .FirstOrDefault();
                if (channel == null)
                {
                    continue;
                }

                try
                {
                    // Send the message
                    _logger.LogTrace($"Sending messsage \"{message ?? title}\" (guild: {guild.Name} #{guild.Id}, channel: {channel.Name})");
                    var embed = BuildEmbed(title, description, fields, fieldsInline, url, thumbnailUrl, imageUrl, color);
                    var msg = await channel.SendMessageAsync(text: message, embed: embed);
                    if (msg == null)
                    {
                        throw new Exception($"Unable to send message \"{message ?? title}\" (guild: {guild.Name} #{guild.Id}, channel: {channel.Name})");
                    }

                    // NOTE: We only want to send one message per guild, so exit after the first one has sent successfully
                    return msg.Id;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, ex.Message);
                    continue;
                }
            }

            _logger.LogWarning($"Unable to find any suitable channels to post message (guild: {guild.Name} #{guild.Id}, channel patterns: {String.Join(",", channelPatterns)})");
            return 0;
        }

        public IDisposable SubscribeToReplies(ulong messageId, Func<IMessage, bool> filter, Func<IMessage, Task> onReply)
        {
            var replyCallback = (Func<SocketMessage, Task>)(
                (msg) =>
                {
                    if (msg.Reference != null && msg.Reference.MessageId.IsSpecified && msg.Reference.MessageId.Value == messageId)
                    {
                        if (filter?.Invoke(msg) != false)
                        {
                            return onReply(msg);
                        }
                    }

                    return Task.CompletedTask;
                }
            );

            _client.MessageReceived += replyCallback;
            return new DisposableDelegate(
                () => _client.MessageReceived -= replyCallback
            );
        }

        public IDisposable SubscribeToReactions(ulong messageId, Func<IUser, IReaction, bool> filter, Func<IMessage, IReaction, Task> onReaction)
        {
            var reactionCallback = (Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, SocketReaction, Task>)(
                (msg, channel, reaction) =>
                {
                    if (reaction.MessageId == messageId)
                    {
                        if (filter?.Invoke(reaction.User.IsSpecified ? reaction.User.Value : null, reaction) != false)
                        {
                            return onReaction(reaction.Message.IsSpecified ? reaction.Message.Value : null, reaction);
                        }
                    }

                    return Task.CompletedTask;
                }
            );

            _client.ReactionAdded += reactionCallback;
            return new DisposableDelegate(
                () => _client.ReactionAdded -= reactionCallback
            );
        }

        private Embed BuildEmbed(string title = null, string description = null, IDictionary<string, string> fields = null, bool fieldsInline = false, string url = null, string thumbnailUrl = null, string imageUrl = null, Color? color = null)
        {
            // Pre-build the embed content (so we can share it across all messages)
            // If the title is not null, we assume the message has emdeded content
            if (!string.IsNullOrEmpty(title))
            {
                var fieldBuilders = new List<EmbedFieldBuilder>();
                if (fields != null)
                {
                    fieldBuilders = fields.Select(x => new EmbedFieldBuilder()
                        .WithName(x.Key)
                        .WithValue(string.IsNullOrEmpty(x.Value) ? "-" : x.Value)
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
                    .WithColor((color != null ? color.Value : Color.Default))
                    .WithFooter(x => x.Text = "https://scmm.app")
                    .Build();
            }

            return null;
        }

        public IEnumerable<DiscordShard> Shards => _client.Shards.Select(x => new DiscordShard(x));

        public IEnumerable<DiscordGuild> Guilds => _client.Shards.SelectMany(x => x.Guilds).Select(x => new DiscordGuild(x));

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
