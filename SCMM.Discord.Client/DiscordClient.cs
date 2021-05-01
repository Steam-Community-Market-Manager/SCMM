using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SCMM.Discord.Client
{
    public class DiscordClient : IDisposable
    {
        private readonly ILogger<DiscordClient> _logger;
        private readonly DiscordConfiguration _configuration;
        private readonly DiscordShardedClient _client;
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
            _client.ShardReady += OnShardReadyAsync;
            _client.JoinedGuild += OnJoinedGuildSayHello;
            _client.JoinedGuild += (x) => Task.Run(() => GuildJoined?.Invoke(new DiscordGuild(x)));
            _client.LeftGuild += (x) => Task.Run(() => GuildLeft?.Invoke(new DiscordGuild(x)));
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

        public Task SetWatchingStatus(string status)
        {
            return _client.SetGameAsync(
                status, null, ActivityType.Watching
            );
        }

        public Task BroadcastMessageAsync(string message, string title = null, string description = null, IDictionary<string, string> fields = null, string url = null, string thumbnailUrl = null, string imageUrl = null, System.Drawing.Color? color = null)
        {
            return BroadcastMessageAsync(null, null, message, title: title, description: description, fields: fields, url: url, thumbnailUrl: thumbnailUrl, imageUrl: imageUrl, color: color);
        }

        public Task BroadcastMessageAsync(string channelPattern, string message, string title = null, string description = null, IDictionary<string, string> fields = null, string url = null, string thumbnailUrl = null, string imageUrl = null, System.Drawing.Color? color = null)
        {
            return BroadcastMessageAsync(null, channelPattern, message, title: title, description: description, url: url, fields: fields, thumbnailUrl: thumbnailUrl, imageUrl: imageUrl, color: color);
        }

        public async Task BroadcastMessageAsync(string guildPattern, string channelPattern, string message, string title = null, string description = null, IDictionary<string, string> fields = null, bool fieldsInline = false, string url = null, string thumbnailUrl = null, string imageUrl = null, System.Drawing.Color? color = null)
        {
            // If guild is null, we'll broadcast to all guilds
            var guilds = _client.Guilds
                .OrderBy(x => x.Name)
                .Where(x => String.IsNullOrEmpty(guildPattern) || Regex.IsMatch(x.Id.ToString(), guildPattern) || Regex.IsMatch(x.Name, guildPattern));
            foreach (var guild in guilds)
            {
                // If the channel is null, we'll broadcast to the first channel that we have permission to
                var channels = guild.TextChannels
                    .OrderBy(x => x.Name)
                    .Where(x => String.IsNullOrEmpty(channelPattern) || String.Equals($"<#{x.Id}>", channelPattern, StringComparison.InvariantCultureIgnoreCase) || Regex.IsMatch(x.Name, channelPattern))
                    .Where(x => guild.CurrentUser.GetPermissions(x).SendMessages)
                    .ToList();
                if (!channels.Any() && !String.IsNullOrEmpty(channelPattern))
                {
                    // If the channel pattern didn't match anything, broadcast to the first channel that we havve permission to
                    var firstChannel = guild.TextChannels.OrderBy(x => x.Name).FirstOrDefault(x => guild.CurrentUser.GetPermissions(x).SendMessages);
                    if (firstChannel != null)
                    {
                        channels.Add(firstChannel);
                    }
                }

                foreach (var channel in channels)
                {
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

        public IEnumerable<DiscordShard> Shards => _client.Shards.Select(x => new DiscordShard(x));

        public IEnumerable<DiscordGuild> Guilds => _client.Guilds.Select(x => new DiscordGuild(x));

        public DiscordUser User => new DiscordUser(_client.CurrentUser);

        // Events
        public event Action<IEnumerable<DiscordGuild>> Ready;
        public event Action<DiscordGuild> GuildJoined;
        public event Action<DiscordGuild> GuildLeft;
    }
}
