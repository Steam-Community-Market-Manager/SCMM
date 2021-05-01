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

            using (var scope = _services.CreateScope())
            {
                // Execute the command
                var context = new ShardedCommandContext(_client, message);
                var result = await _commands.ExecuteAsync(
                    context: context,
                    argPos: commandArgPos,
                    services: scope.ServiceProvider
                );
            }
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
            }
            else
            {
                switch (result.Error)
                {
                    // Warning - bad request
                    case CommandError.UnknownCommand:
                    case CommandError.MultipleMatches:
                    case CommandError.ObjectNotFound:
                    case CommandError.BadArgCount:
                    case CommandError.ParseFailed:
                        {
                        _logger.LogWarning(
                            $"Command '{commandName}' failed (guild: {guildName}, channel: {channelName}, user: {userName}). Reason: {result.Error.Value} {result.ErrorReason}"
                        );
                        await context.Channel.SendMessageAsync($"Sorry, I don't understand your command 😕");
                        break;
                    }

                    // Warning - insufficent permissions
                    case CommandError.UnmetPrecondition:
                    {
                        _logger.LogWarning(
                            $"Command '{commandName}' failed (guild: {guildName}, channel: {channelName}, user: {userName}). Reason: {result.Error.Value} {result.ErrorReason}"
                        );
                        await context.Channel.SendMessageAsync($"Sorry, you don't have permission to do that 😕");
                        break;
                    }

                    // Error - something went wrong...
                    case CommandError.Exception:
                    case CommandError.Unsuccessful:
                    {
                        _logger.LogError(
                            $"Command '{commandName}' failed (guild: {guildName}, channel: {channelName}, user: {userName}). Reason: {result.Error.Value} {result.ErrorReason}"
                        );
                        await context.Channel.SendMessageAsync($"Sorry, something went horribly wrong 😕");
                        break;
                    }
                }
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
