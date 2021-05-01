using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SCMM.Discord.Client
{
    internal class DiscordCommandHandler
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _services;
        private readonly CommandService _commands;
        private readonly DiscordShardedClient _client;
        private readonly DiscordConfiguration _configuration;

        public DiscordCommandHandler(ILogger logger, IServiceProvider services, CommandService commands, DiscordShardedClient client, DiscordConfiguration configuration)
        {
            _logger = logger;
            _services = services;
            _commands = commands;
            _client = client;
            _configuration = configuration;
        }

        public async Task AddCommandsAsync()
        {
            _client.MessageReceived += OnMessageReceivedAsync;
            _commands.CommandExecuted += OnCommandExecutedAsync;
            _commands.Log += OnCommandLogAsync;

            try
            {
                using (var scope = _services.CreateScope())
                {
                    await _commands.AddModulesAsync(
                        assembly: Assembly.GetEntryAssembly(),
                        services: scope.ServiceProvider
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialise command handlers, likely due to invalid dependencies");
            }
        }

        private async Task OnMessageReceivedAsync(SocketMessage msg)
        {
            // Don't process the command if it was a system message
            var message = msg as SocketUserMessage;
            if (message == null)
            {
                return;
            }

            // Determine if the message is a command based on the prefix and make sure other bots don't trigger our commands
            int commandArgPos = 0;
            if (!(message.HasCharPrefix(_configuration.CommandPrefix.FirstOrDefault(), ref commandArgPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref commandArgPos)) ||
                message.Author.IsBot)
            {
                return;
            }

            // Execute the command in a background thread to avoid clogging the gateway thread
            _ = Task.Run(async () =>
            {
                using (var scope = _services.CreateScope())
                {
                    var context = new ShardedCommandContext(_client, message);
                    var result = await _commands.ExecuteAsync(
                        context: context,
                        argPos: commandArgPos,
                        services: scope.ServiceProvider
                    );
                }
            });
        }

        public async Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            var commandName = command.IsSpecified ? ($"{command.Value?.Module?.Name} {command.Value?.Name}").Trim() : "unspecified";
            var userName = $"{context.User.Username} #{context.User.Discriminator}";
            var guildName = context.Guild.Name;
            var channelName = context.Channel.Name;
            if (result.IsSuccess)
            {
                // Success
                _logger.LogInformation(
                    $"Command '{commandName}' executed successfully (guild: {guildName}, channel: {channelName}, user: {userName})"
                );

                await context.Message.AddReactionAsync(new Emoji("👌")); // ok
            }
            else
            {
                var logLevel = LogLevel.Error;
                var responseMessage = Task.CompletedTask;
                var reactionEmoji = new Emoji("😢"); // cry
                switch (result.Error)
                {
                    case CommandError.UnknownCommand:
                    {
                        logLevel = LogLevel.Warning;
                        responseMessage = context.Channel.SendMessageAsync($"Sorry, I don't understand that command 😕 use `{_configuration.CommandPrefix}help` for a list of support commands");
                        break;
                    }
                    case CommandError.ParseFailed:
                    {
                        logLevel = LogLevel.Warning;
                        responseMessage = context.Channel.SendMessageAsync($"Sorry, your command contains invalid characters or objects that I can't understand 😕");
                        break;
                    }
                    case CommandError.BadArgCount:
                    {
                        logLevel = LogLevel.Warning;
                        responseMessage = context.Channel.SendMessageAsync($"Sorry, your command has an invalid number of parameters 😕 use `{_configuration.CommandPrefix}help` for details on command usage");
                        break;
                    }
                    case CommandError.ObjectNotFound:
                    {
                        logLevel = LogLevel.Warning;
                        responseMessage = context.Channel.SendMessageAsync($"Sorry, I'm supposed to be able to understand that command, but I can't find the code that should handle it 😅");
                        break;
                    }
                    case CommandError.MultipleMatches:
                    {
                        logLevel = LogLevel.Warning;
                        responseMessage = context.Channel.SendMessageAsync($"Sorry, your command is ambiguous, try be more specific 😕 use `{_configuration.CommandPrefix}help` for details on command usage");
                        break;
                    }
                    case CommandError.UnmetPrecondition:
                    {
                        logLevel = LogLevel.Warning;
                        responseMessage = context.Channel.SendMessageAsync($"Sorry, you don't have permission to do that 🚫");
                            reactionEmoji = new Emoji("🚫"); // prohibited
                            break;
                    }
                    case CommandError.Exception:
                    {
                        logLevel = LogLevel.Error;
                        responseMessage = context.Channel.SendMessageAsync($"Sorry, something terrible went wrong your command cannot be completed right now 😵 try again later");
                        reactionEmoji = new Emoji("🐛"); // bug
                        break;
                    }
                    case CommandError.Unsuccessful:
                    {
                        logLevel = LogLevel.Error;
                        responseMessage = context.Channel.SendMessageAsync($"Sorry, your command cannot be completed right now and I'm unsure why 😵 try again later");
                        reactionEmoji = new Emoji("🐛"); // bug
                        break;
                    }
                }

                _logger.Log(logLevel,
                    $"Command '{commandName}' failed (guild: {guildName}, channel: {channelName}, user: {userName}). Reason: {result.Error.Value} {result.ErrorReason}. The original message was \"{context.Message.Content}\""
                );

                await context.Message.AddReactionAsync(reactionEmoji);
                await responseMessage;
            }
        }

        public Task OnCommandLogAsync(LogMessage message)
        {
            if (message.Exception is CommandException commandException)
            {
                var context = commandException.Context;
                var command = commandException.Command;
                var commandName = ($"{command?.Module?.Name} {command?.Name}").Trim();
                var userName = $"{context.User.Username} #{context.User.Discriminator}";
                var guildName = context.Guild.Name;
                var channelName = context.Channel.Name;
                _logger.LogError(
                    commandException,
                    $"Command '{commandName}' triggered an exception (guild: {guildName}, channel: {channelName}, user: {userName})"
                );
            }

            return Task.CompletedTask;
        }
    }
}
