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
    public class DiscordCommandHandler
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _services;
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;
        private readonly DiscordConfiguration _configuration;

        public DiscordCommandHandler(ILogger logger, IServiceProvider services, CommandService commands, DiscordSocketClient client, DiscordConfiguration configuration)
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
                _logger.LogError(ex, "Failed to initialise command handlers");
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
                var context = new SocketCommandContext(_client, message);
                var result = await _commands.ExecuteAsync(
                    context: context,
                    argPos: commandArgPos,
                    services: scope.ServiceProvider
                );
            }
        }

        public Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            var commandName = command.IsSpecified ? command.Value.Name : "unspecified";
            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    $"Discord command '{commandName}' success (guild: {context.Guild.Name}, channel: {context.Channel.Name}, user: {context.User.Username})"
                );
            }
            else if (result.Error == CommandError.Exception)
            {
                _logger.LogError(
                    $"Discord command '{commandName}' exeception occurred (guild: {context.Guild.Name}, channel: {context.Channel.Name}, user: {context.User.Username}). Reason: {result.ErrorReason}"
                );
                return context.Channel.SendMessageAsync($"Something went horribly wrong 😕");
            }
            else if (result.Error != CommandError.UnknownCommand)
            {
                _logger.LogWarning(
                    $"Discord command '{commandName}' failed (guild: {context.Guild.Name}, channel: {context.Channel.Name}, user: {context.User.Username}). Reason: {result.ErrorReason}"
                );
            }

            return Task.CompletedTask;
        }

        public Task OnCommandLogAsync(LogMessage message)
        {
            if (message.Exception is CommandException commandException)
            {
                var command = commandException.Command;
                var context = commandException.Context;
                _logger.LogError(
                    commandException,
                    $"Discord command '{command.Name}' unhandled exception (guild: {context.Guild.Name}, channel: {context.Channel.Name}, user: {context.User.Username})"
                );
            }

            return Task.CompletedTask;
        }
    }
}
