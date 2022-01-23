using Azure.AI.TextAnalytics;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Azure.AI;
using SCMM.Discord.Client.Commands;
using SCMM.Discord.Client.Extensions;
using System.Reflection;

namespace SCMM.Discord.Client
{
    internal class DiscordCommandHandler
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _services;
        private readonly CommandService _commands;
        private readonly DiscordShardedClient _client;
        private readonly DiscordConfiguration _configuration;

        public DiscordCommandHandler(ILogger logger, IServiceProvider services, DiscordShardedClient client, DiscordConfiguration configuration)
        {
            _logger = logger;
            _services = services;
            _commands = new CommandService();
            _client = client;
            _configuration = configuration;
        }

        public async Task AddCommandsAsync(params Assembly[] assemblies)
        {
            _client.MessageReceived += OnMessageReceivedAsync;
            _commands.CommandExecuted += OnCommandExecutedAsync;
            _commands.Log += OnLogAsync;

            try
            {
                if (!assemblies.Any())
                {
                    assemblies = new Assembly[]
                    {
                        Assembly.GetEntryAssembly()
                    };
                }

                using var scope = _services.CreateScope();
                {
                    foreach (var assembly in assemblies)
                    {
                        await _commands.AddModulesAsync(
                            assembly: assembly,
                            services: scope.ServiceProvider
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialise command modules, likely due to invalid dependencies");
            }
        }

        private Task OnLogAsync(LogMessage message)
        {
            if (message.Exception is CommandException commandException)
            {
                var context = commandException.Context;
                var command = commandException.Command;
                var commandName = ($"{command?.Module?.Name} {command?.Name}").Trim();
                var userName = context.User.GetFullUsername();
                var guildName = (context.Guild != null ? $"{context.Guild.Name} #{context.Guild.Id}" : "n/a");
                var channelName = (context.Channel != null ? context.Channel.Name : "n/a");
                _logger.LogError(
                    commandException,
                    $"Command '{commandName}' triggered an exception (guild: {guildName}, channel: {channelName}, user: {userName})"
                );
            }

            return Task.CompletedTask;
        }

        private Task OnMessageReceivedAsync(SocketMessage msg)
        {
            // Don't process the command if it was a system message
            var message = msg as SocketUserMessage;
            if (message == null)
            {
                return Task.CompletedTask;
            }

            // Ignore messages from other robots
            if (message.Author.IsBot || message.Author.IsWebhook)
            {
                return Task.CompletedTask;
            }

            // Ignore messages we don't have permission to view
            if (String.IsNullOrEmpty(message.Content))
            {
                return Task.CompletedTask;
            }

            // If we are mentioned, react to the message before handling it
            if (message.Content.Contains(_client.CurrentUser.Username, StringComparison.InvariantCultureIgnoreCase) ||
                message.MentionedUsers.Contains(_client.CurrentUser))
            {
                _ = ReactToMessageSentiment(message);
                _ = RelayMentionedMessage(message);
            }

            // If a command is detected, execute it
            var commandArgPos = 0;
            if (message.HasCharPrefix(_configuration.CommandPrefix.FirstOrDefault(), ref commandArgPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref commandArgPos))
            {
                // Execute the command in a background thread to avoid clogging the gateway thread
                _ = Task.Run(async () =>
                {
                    using var scope = _services.CreateScope();
                    var context = new ShardedCommandContext(_client, message);
                    var result = await _commands.ExecuteAsync(
                        context: context,
                        argPos: commandArgPos,
                        services: scope.ServiceProvider
                    );
                });
            }

            return Task.CompletedTask;
        }

        private Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            return RespondToCommandResult(
                (command.IsSpecified ? ($"{command.Value?.Module?.Name} {command.Value?.Name}").Trim() : "unspecified"),
                (context.User.GetFullUsername()),
                (context.Guild != null ? $"{context.Guild.Name} #{context.Guild.Id}" : "n/a"),
                (context.Channel != null ? context.Channel.Name : "n/a"),
                (result as CommandResult),
                (text, embed) => context.Message.ReplyAsync(text: text, embed: embed),
                (emote) => context.Message.TryAddReactionAsync(emote)
            );
        }

        private async Task RespondToCommandResult<T>(string commandName, string userName, string guildName, string channelName, IResult result, Func<string, Embed, Task<T>> replyFunc, Func<Emoji, Task> addReactionFunc)
            where T : IUserMessage
        {
            var commandResult = (result as CommandResult);
            if (result == null)
            {
                return;
            }

            // Success
            if (result.IsSuccess)
            {
                _logger.LogTrace(
                    $"Command '{commandName}' executed successfully (guild: {guildName}, channel: {channelName}, user: {userName})"
                );

                if (commandResult?.Reaction != null)
                {
                    await addReactionFunc(commandResult.Reaction);
                }
                if (commandResult?.Reason != null || commandResult?.Embed != null)
                {
                    await replyFunc(commandResult.Reason, commandResult.Embed);
                }
            }

            // Error gracefully reported by the command handler
            else if (result.Error == CommandError.Unsuccessful && commandResult != null)
            {
                _logger.LogTrace(
                    $"Command '{commandName}' had an unsuccessful outcome (guild: {guildName}, channel: {channelName}, user: {userName}). {commandResult.Reason}"
                );

                if (commandResult?.Reaction != null)
                {
                    await addReactionFunc(commandResult.Reaction);
                }
                if (commandResult?.Reason != null || commandResult?.Explaination != null)
                {
                    await replyFunc(
                        (commandResult.Explaination != null) ? null : commandResult.Reason,
                        (commandResult.Explaination == null) ? null : new EmbedBuilder()
                            .WithTitle(commandResult.Reason)
                            .WithDescription(commandResult.Explaination)
                            .WithUrl(commandResult.HelpUrl)
                            .WithImageUrl(commandResult.HelpImageUrl)
                            .Build()
                    );
                }
            }

            // Unhandled error thrown by the command handler
            else
            {
                var logLevel = LogLevel.Error;
                var responseMessage = Task.CompletedTask;
                var reactionEmoji = new Emoji("😢"); // cry
                switch (result.Error)
                {
                    case CommandError.UnknownCommand:
                        logLevel = LogLevel.Warning;
                        responseMessage = replyFunc($"Sorry, I don't understand that command 😕", null);
                        break;

                    case CommandError.ParseFailed:
                        logLevel = LogLevel.Warning;
                        responseMessage = replyFunc($"Sorry, your command contains invalid characters or objects that I can't understand 😕", null);
                        break;

                    case CommandError.BadArgCount:
                        logLevel = LogLevel.Warning;
                        responseMessage = replyFunc($"Sorry, your command has an invalid number of parameters 😕", null);
                        break;

                    case CommandError.ObjectNotFound:
                        logLevel = LogLevel.Warning;
                        responseMessage = replyFunc($"Sorry, I'm supposed to be able to understand that command, but I can't find the code that should handle it 😅", null);
                        break;

                    case CommandError.MultipleMatches:
                        logLevel = LogLevel.Warning;
                        responseMessage = replyFunc($"Sorry, your command is ambiguous, try be more specific 😕", null);
                        break;

                    case CommandError.UnmetPrecondition:
                        logLevel = LogLevel.Warning;
                        responseMessage = replyFunc($"Sorry, {result.ErrorReason}", null);
                        break;

                    case CommandError.Exception:
                        logLevel = LogLevel.Error;
                        responseMessage = replyFunc($"Sorry, something terrible went wrong your command cannot be completed right now 😵 try again later", null);
                        reactionEmoji = new Emoji("🐛"); // bug
                        break;

                    case CommandError.Unsuccessful:
                        logLevel = LogLevel.Error;
                        responseMessage = replyFunc($"Sorry, your command cannot be completed right now and I'm unsure why 😵 try again later", null);
                        reactionEmoji = new Emoji("🐛"); // bug
                        break;
                }

                _logger.Log(logLevel,
                    $"Command '{commandName}' failed (guild: {guildName}, channel: {channelName}, user: {userName}). Reason: {result.Error.Value} {result.ErrorReason}"
                );

                await addReactionFunc(reactionEmoji);
                await responseMessage;
            }
        }

        private async Task ReactToMessageSentiment(SocketUserMessage message)
        {
            try
            {
                using var scope = _services.CreateScope();
                var azureAiClient = _services.GetRequiredService<AzureAiClient>();
                var sentiment = await azureAiClient.GetTextSentimentAsync(message.Content);
                var reactions = new List<Emoji>();
                switch (sentiment)
                {
                    case TextSentiment.Positive:
                        reactions.Add(new Emoji("🥰"));
                        reactions.Add(new Emoji("😘"));
                        reactions.Add(new Emoji("💋"));
                        reactions.Add(new Emoji("❤️"));
                        break;
                    case TextSentiment.Neutral:
                        reactions.Add(new Emoji("👋"));
                        reactions.Add(new Emoji("👍"));
                        reactions.Add(new Emoji("👌"));
                        break;
                    case TextSentiment.Negative:
                        reactions.Add(new Emoji("🖕"));
                        reactions.Add(new Emoji("💩"));
                        reactions.Add(new Emoji("😠"));
                        break;
                    case TextSentiment.Mixed:
                        reactions.Add(new Emoji("😞"));
                        reactions.Add(new Emoji("😟"));
                        reactions.Add(new Emoji("💔"));
                        break;
                }
                _ = message.TryAddReactionAsync(
                    reactions.ElementAt(Random.Shared.Next(reactions.Count))
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to react to message");
            }
        }

        private async Task RelayMentionedMessage(SocketMessage message)
        {
            try
            {
                var guildChannel = (message.Channel as SocketGuildChannel);
                if (guildChannel == null)
                {
                    return;
                }

                var userName = message.Author.GetFullUsername();
                var guildName = (guildChannel.Guild != null ? $"{guildChannel.Guild.Name} #{guildChannel.Guild.Id}" : "n/a");
                var channelName = (message.Channel != null ? message.Channel.Name : "n/a");
                var content = message.Content;

                var notifyGuild = _client.GetGuild(761035706021314561);
                if (notifyGuild != null)
                {
                    var notifyChannel = notifyGuild.TextChannels.FirstOrDefault(x => x.Name == "bot-mentions");
                    if (notifyChannel != null)
                    {
                        var embed = new EmbedBuilder()
                            .WithTitle("SCMM was mentioned somewhere")
                            .WithFields(new []
                            {
                                new EmbedFieldBuilder().WithName("User").WithValue(userName),
                                new EmbedFieldBuilder().WithName("Guild").WithValue(guildName),
                                new EmbedFieldBuilder().WithName("Channel").WithValue(channelName),
                                new EmbedFieldBuilder().WithName("Content").WithValue(content)
                            })
                            .WithTimestamp(message.Timestamp);

                        await notifyChannel.SendMessageAsync(
                            embed: embed.Build()
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to relay a mentioned message");
            }
        }
    }
}
