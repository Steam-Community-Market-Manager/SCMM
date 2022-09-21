using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Discord.Client.Extensions;
using SCMM.Shared.API.Extensions;
using SCMM.Shared.Client;
using System.Text.RegularExpressions;

namespace SCMM.Discord.Client
{
    public class DiscordClient : IDisposable
    {
        private readonly ILogger<DiscordClient> _logger;
        private readonly string _websiteUrl;
        private readonly DiscordConfiguration _configuration;
        private readonly DiscordShardedClient _client;
        private readonly ManualResetEvent _clientIsConnected;
        private readonly DiscordCommandHandler _commandHandler;
        private readonly DiscordInteractionHandler _interactionHandler;
        private bool handlersRegistered;
        private bool disposedValue;

        public DiscordClient(ILogger<DiscordClient> logger, IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _websiteUrl = configuration.GetWebsiteUrl();
            _configuration = configuration.GetDiscordConfiguration();
            _client = new DiscordShardedClient(new DiscordSocketConfig()
            {
                ShardId = _configuration.ShardId,
                TotalShards = _configuration.TotalShards,
                // TODO: Apply for guild memebers privileged intent
                //GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers,
                AlwaysDownloadUsers = true,
                SuppressUnknownDispatchWarnings = true                
            });
            _client.Log += OnClientLogAsync;
            _client.ShardConnected += OnShardConnectedAsync;
            _client.ShardDisconnected += OnShardDisconnectedAsync;
            _client.ShardReady += OnShardReadyAsync;
            _client.JoinedGuild += OnJoinedGuildSayHello;
            _client.JoinedGuild += (x) => Task.Run(() => GuildJoined?.Invoke(new DiscordGuild(x)));
            _client.LeftGuild += (x) => Task.Run(() => GuildLeft?.Invoke(new DiscordGuild(x)));
            _clientIsConnected = new ManualResetEvent(false);
            _commandHandler = new DiscordCommandHandler(logger, serviceProvider, _client, _configuration);
            _interactionHandler = new DiscordInteractionHandler(logger, serviceProvider, _client, _configuration);
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

        public async Task<IEnumerable<DiscordMessage>> GetMessagesAsync(ulong guildId, ulong channelId, int messageLimit = 100)
        {
            WaitUntilClientIsConnected();

            // Find the guild
            var guild = _client.GetGuild(guildId);
            if (guild == null)
            {
                throw new Exception($"Unable to find guild (id: {guildId})");
            }

            // Find the channel
            var channel = guild.GetTextChannel(channelId);
            if (channel == null)
            {
                throw new Exception($"Unable to find guild channel (guildId: {guildId}, channelId: {channelId})");
            }

            // Get the messages
            var results = new List<DiscordMessage>();
            var messages = channel.GetMessagesAsync(limit: messageLimit);
            await foreach (var message in messages)
            {
                results.AddRange(
                    message.Select(m => new DiscordMessage
                    {
                        Id = m.Id,
                        AuthorId = m.Author.Id,
                        Content = m.Content,
                        Attachments = m.Attachments.Select(a => new DiscordMessageAttachment
                        {
                            Id = a.Id,
                            Url = a.Url,
                            FileName = a.Filename,
                            ContentType = a.ContentType,
                            Description = a.Description
                        }),
                        Timestamp = m.Timestamp,
                    })
                );
            }

            return results;
        }

        public async Task<ulong> SendMessageAsync(
            string userIdOrName, string message = null,
            string authorIconUrl = null, string authorName = null, string authorUrl = null, 
            string title = null, string url = null, string thumbnailUrl = null, 
            string description = null, IDictionary<string, string> fields = null, bool fieldsInline = false, 
            string imageUrl = null, Color? color = null, string[] reactions = null)
        {
            WaitUntilClientIsConnected();

            // Find the user
            var usernameParts = userIdOrName.Split("#", StringSplitOptions.TrimEntries);
            var user = (usernameParts.Length > 1)
                ? _client.GetUser(usernameParts.FirstOrDefault(), usernameParts.LastOrDefault())
                : _client.GetUser(UInt64.Parse(userIdOrName));
            if (user == null)
            {
                throw new Exception($"Unable to find user for message \"{message ?? title}\" (username: {userIdOrName})");
            }

            // Create a DM channel with the user
            var dm = await user.CreateDMChannelAsync();
            if (dm == null)
            {
                throw new Exception($"Unable to create DM channel for message \"{message ?? title}\" (username: {userIdOrName})");
            }

            var embed = BuildEmbed(
                authorIconUrl, authorName, authorUrl, title, url, thumbnailUrl, description, fields, fieldsInline, imageUrl, color
            );

            // Send the message
            var msg = await dm.SendMessageAsync(text: message, embed: embed);
            if (msg == null)
            {
                throw new Exception($"Unable to send message \"{message ?? title}\" (username: {userIdOrName})");
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

        public async Task<ulong> SendMessageAsync(
            ulong guildId, ulong channelId, string message = null, 
            string authorIconUrl = null, string authorName = null, string authorUrl = null, 
            string title = null, string url = null, string thumbnailUrl = null, 
            string description = null, IDictionary<string, string> fields = null, bool fieldsInline = false, 
            string imageUrl = null, Color? color = null, string[] reactions = null, bool crossPost = false)
        {
            WaitUntilClientIsConnected();

            var guild = _client.GetGuild(guildId);
            if (guild == null)
            {
                throw new Exception($"Unable to find guild (id: {guildId})");
            }

            var channel = guild.TextChannels.FirstOrDefault(x => x.Id == channelId);
            if (channel == null)
            {
                throw new Exception($"Unable to find guild channel (guildId: {guildId}, channelId: {channelId})");
            }

            var embed = BuildEmbed(
                authorIconUrl, authorName, authorUrl, title, url, thumbnailUrl, description, fields, fieldsInline, imageUrl, color
            );

            // Send the message
            _logger.LogTrace($"Sending messsage \"{message ?? title}\" (guild: {guild.Name} #{guild.Id}, channel: {channel.Name})");
            var msg = await channel.SendMessageAsync(text: message, embed: embed);
            if (msg == null)
            {
                throw new Exception($"Unable to send message \"{message ?? title}\" (guild: {guild.Name} #{guild.Id}, channel: {channel.Name})");
            }
                    
            // React to the message
            if (reactions?.Any() == true)
            {
                foreach (var reaction in reactions)
                {
                    await msg.TryAddReactionAsync(new Emoji(reaction));
                }
            }

            // Cross-post (publish) the message
            if (crossPost)
            {
                await msg.CrosspostAsync();
            }

            // NOTE: We only want to send one message per guild, so exit after the first one has sent successfully
            return msg.Id;
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

        private Embed BuildEmbed(
            string authorIconUrl = null, string authorName = null, string authorUrl = null, 
            string title = null, string url = null, string thumbnailUrl = null, 
            string description = null, IDictionary<string, string> fields = null, bool fieldsInline = false, 
            string imageUrl = null, Color? color = null)
        {
            // Pre-build the embed content (so we can share it across all messages)
            // If the title is not null, we assume the message has emdeded content
            if (!string.IsNullOrEmpty(title))
            {
                var fieldBuilders = new List<EmbedFieldBuilder>();
                if (fields != null && fields.Any())
                {
                    fieldBuilders = fields.Select(x => new EmbedFieldBuilder()
                        .WithName(x.Key)
                        .WithValue(string.IsNullOrEmpty(x.Value) ? "-" : x.Value)
                        .WithIsInline(fieldsInline)
                    ).ToList();
                }

                var embed = new EmbedBuilder()
                    .WithTitle(title)
                    .WithUrl(SafeUrl(url))
                    .WithThumbnailUrl(SafeUrl(thumbnailUrl))
                    .WithDescription(description)
                    .WithFields(fieldBuilders)
                    .WithImageUrl(SafeUrl(imageUrl))
                    .WithColor((color != null ? color.Value : Color.Default))
                    .WithFooter(x => x.Text = SafeUrl(_websiteUrl));

                if (!String.IsNullOrEmpty(authorName))
                {
                    embed.WithAuthor(authorName, SafeUrl(authorIconUrl), SafeUrl(authorUrl));
                }

                return embed.Build();
            }

            return null;
        }

        private string SafeUrl(string url)
        {
            // Discord throws "INVALID_URL" errors if unescape characters or query strings are present
            if (String.IsNullOrEmpty(url))
            {
                return null;
            }
            var urlBuilder = new UriBuilder(url)
            {
                Query = String.Empty 
            };

            return urlBuilder.ToString();
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
