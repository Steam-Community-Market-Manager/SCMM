using Azure.AI.TextAnalytics;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Azure.AI;
using SCMM.Discord.Client.Extensions;
using System.Reflection;

namespace SCMM.Discord.Client
{
    internal class DiscordCommandHandler
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _services;
        private readonly CommandService _commands;
        private readonly HashSet<SlashCommandCallback> _slashCommands;
        private readonly DiscordShardedClient _client;
        private readonly DiscordConfiguration _configuration;

        public DiscordCommandHandler(ILogger logger, IServiceProvider services, DiscordShardedClient client, DiscordConfiguration configuration)
        {
            _logger = logger;
            _services = services;
            _commands = new CommandService();
            _slashCommands = new HashSet<SlashCommandCallback>();
            _client = client;
            _configuration = configuration;
        }

        public async Task AddCommandsAsync(params Assembly[] assemblies)
        {
            _client.MessageReceived += OnMessageReceivedAsync;
            _commands.CommandExecuted += OnCommandExecutedAsync;
            _commands.Log += OnCommandLogAsync;

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
                _logger.LogError(ex, "Failed to initialise command handlers, likely due to invalid dependencies");
            }
        }

        public async Task AddSlashCommandsAsync(params Assembly[] assemblies)
        {
            _client.SlashCommandExecuted += OnSlashCommandExecutedAsync;
            
            try
            {
                if (!assemblies.Any())
                {
                    assemblies = new Assembly[]
                    {
                        Assembly.GetEntryAssembly()
                    };
                }

                foreach (var assembly in assemblies)
                {
                    // Discover command modules
                    var slashCommandModules = assembly.GetTypes()
                        .Where(x => typeof(ISlashCommandModule).IsAssignableFrom(x)).Select(x => new
                        {
                            Type = x,
                            Group = x.GetCustomAttribute<GroupAttribute>(),
                            Summary = x.GetCustomAttribute<SummaryAttribute>()
                        })
                        .ToList();

                    // Discover commands
                    foreach (var slashCommandModule in slashCommandModules)
                    {
                        var moduleSlashCommands = slashCommandModule.Type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                            .Where(x => x.GetCustomAttribute<CommandAttribute>() != null)
                            .Select(x => new
                            {
                                Module = slashCommandModule.Type,
                                Method = x,
                                Command = x.GetCustomAttribute<CommandAttribute>(),
                                Summary = x.GetCustomAttribute<SummaryAttribute>()
                            })
                            .ToList();

                        // Command group
                        if (slashCommandModule.Group != null)
                        {
                            var subcommandBuilders = new List<SlashCommandOptionBuilder>();
                            foreach (var moduleSlashCommand in moduleSlashCommands)
                            {
                                var parameterBuilders = moduleSlashCommand.Method.GetParameters()
                                    .Where(x => x.GetCustomAttribute<NameAttribute>() != null)
                                    .Select(x => new SlashCommandOptionBuilder()
                                        .WithName(x.GetCustomAttribute<NameAttribute>()?.Text ?? String.Empty)
                                        .WithDescription(x.GetCustomAttribute<SummaryAttribute>()?.Text ?? String.Empty)
                                        .WithType(x.ParameterType.ToCommandOptionType())
                                    );

                                subcommandBuilders.Add(new SlashCommandOptionBuilder()
                                    .WithName(moduleSlashCommand.Command.Text)
                                    .WithDescription(moduleSlashCommand.Summary?.Text ?? String.Empty)
                                    .WithType(ApplicationCommandOptionType.SubCommand)
                                    //.AddOptions(parameterBuilders.ToArray())
                                );
                            }

                            var commandBuilder = new SlashCommandBuilder()
                                .WithName(slashCommandModule.Group.Prefix)
                                .WithDescription(slashCommandModule.Summary?.Text ?? String.Empty)
                                .AddOptions(subcommandBuilders.ToArray());

                            _slashCommands.Add(new SlashCommandCallback()
                            {
                                CommandName = commandBuilder.Name,
                                Command = commandBuilder.Build(),
                                Module = slashCommandModule.Type
                            });
                        }

                        // Root commands
                        else
                        {
                            foreach (var moduleSlashCommand in moduleSlashCommands)
                            {
                                var parameterBuilders = moduleSlashCommand.Method.GetParameters()
                                    .Where(x => x.GetCustomAttribute<NameAttribute>() != null)
                                    .Select(x => new SlashCommandOptionBuilder()
                                        .WithName(x.GetCustomAttribute<NameAttribute>()?.Text ?? String.Empty)
                                        .WithDescription(x.GetCustomAttribute<SummaryAttribute>()?.Text ?? String.Empty)
                                        .WithType(x.ParameterType.ToCommandOptionType())
                                        .WithRequired(!x.IsOptional)
                                    );

                                var commandBuilder = new SlashCommandBuilder()
                                    .WithName(moduleSlashCommand.Command.Text)
                                    .WithDescription(moduleSlashCommand.Summary?.Text ?? String.Empty)
                                    .AddOptions(parameterBuilders.ToArray());

                                _slashCommands.Add(new SlashCommandCallback()
                                {
                                    CommandName = commandBuilder.Name,
                                    Command = commandBuilder.Build(),
                                    Module = slashCommandModule.Type
                                });
                            }
                        }
                    }

                    // Update global command registrations
                    /*await _client.Rest.DeleteAllGlobalCommandsAsync();
                    foreach (var globalCommand in _slashCommands)
                    {
                        await _client.Rest.CreateGlobalCommand(globalCommand.Command);
                    }*/
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialise slash command handlers, likely due to invalid dependencies");
            }
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

            // If we are mentioned, react to the message before handling it
            if (message.Content.Contains(_client.CurrentUser.Username, StringComparison.InvariantCultureIgnoreCase) ||
                message.MentionedUsers.Contains(_client.CurrentUser))
            {
                _ = ReactToMessageSentiment(message);
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

        public Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            return RespondToCommand(
                (command.IsSpecified ? ($"{command.Value?.Module?.Name} {command.Value?.Name}").Trim() : "unspecified"),
                (context.User.GetFullUsername()),
                (context.Guild != null ? $"{context.Guild.Name} #{context.Guild.Id}" : "n/a"),
                (context.Channel != null ? context.Channel.Name : "n/a"),
                (result as CommandResult),
                (text, embed) => context.Message.ReplyAsync(text: text, embed: embed),
                (emote) => context.Message.AddReactionSafeAsync(emote)
            );
        }

        public Task OnCommandLogAsync(LogMessage message)
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

        private Task OnSlashCommandExecutedAsync(SocketSlashCommand cmd)
        {
            // Ignore commands from other robots
            if (cmd.User.IsBot || cmd.User.IsWebhook)
            {
                return Task.CompletedTask;
            }

            // Find the callback for this command
            var callback = _slashCommands.FirstOrDefault(x => x.CommandName == cmd.CommandName);
            if (callback == null)
            {
                return Task.CompletedTask;
            }

            // Execute the command in a background thread to avoid clogging the gateway thread
            _ = Task.Run(async () =>
            {
                await cmd.DeferAsync();
                try
                {
                    using var scope = _services.CreateScope();
                    {
                        var handler = scope.ServiceProvider.GetRequiredService(callback.Module);
                        var handlerMethod = callback.Module.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                            .FirstOrDefault(x => x.GetCustomAttribute<CommandAttribute>()?.Text == cmd.CommandName);

                        var parameters = new List<object>();
                        foreach (var parameter in handlerMethod.GetParameters())
                        {
                            var parameterName = parameter.GetCustomAttribute<NameAttribute>()?.Text;
                            var parameterData = cmd.Data.Options.FirstOrDefault(x => x.Name == parameterName);
                            if (parameterData != null)
                            {
                                parameters.Add(parameterData.Value);
                            }
                            else if (parameter.ParameterType == typeof(SocketSlashCommand))
                            {
                                parameters.Add(cmd);
                            }
                            else
                            {
                                parameters.Add(parameter.DefaultValue);
                            }
                        }

                        var result = await ((Task<RuntimeResult>)handlerMethod.Invoke(handler, parameters.ToArray()));
                        await RespondToCommand(
                            (cmd.CommandName),
                            (cmd.User.GetFullUsername()),
                            null,//(cmd.Guild != null ? $"{cmd.Guild.Name} #{cmd.Guild.Id}" : "n/a"),
                            (cmd.Channel != null ? cmd.Channel.Name : "n/a"),
                            (result as CommandResult),
                            (text, embed) => cmd.FollowupAsync(text: text, embed: embed, ephemeral: true),
                            null
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                       ex,
                       $"Command '{cmd.CommandName}' triggered an exception (user: {cmd.User.GetFullUsername()})"
                   );
                    await cmd.FollowupAsync(
                        text: $"Sorry, something terrible went wrong your command cannot be completed right now 😵 try again later",
                        ephemeral: true
                    );
                }
            });

            return Task.CompletedTask;
        }

        private async Task RespondToCommand<T>(string commandName, string userName, string guildName, string channelName, IResult result, Func<string, Embed, Task<T>> replyFunc, Func<Emoji, Task> reactFunc)
            where T : IUserMessage
        {
            if (result == null)
            {
                return;
            }

            var commandResult = (result as CommandResult);

            // Success
            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    $"Command '{commandName}' executed successfully (guild: {guildName}, channel: {channelName}, user: {userName})"
                );

                if (commandResult?.Reaction != null)
                {
                    if (reactFunc != null)
                    {
                        await reactFunc(commandResult.Reaction);
                    }
                    else if (commandResult?.Reason == null && commandResult?.Embed == null)
                    {
                        await replyFunc(commandResult?.Reaction?.Name, null);
                    }
                }
                if (commandResult?.Reason != null || commandResult?.Embed != null)
                {
                    await replyFunc(commandResult.Reason, commandResult.Embed);
                }
            }

            // Error gracefully reported by the command handler
            else if (result.Error == CommandError.Unsuccessful && commandResult != null)
            {
                _logger.LogInformation(
                    $"Command '{commandName}' had an unsuccessful outcome (guild: {guildName}, channel: {channelName}, user: {userName}). {commandResult.Reason}"
                );

                if (commandResult?.Reaction != null)
                {
                    if (reactFunc != null)
                    {
                        await reactFunc(commandResult.Reaction);
                    }
                    else if (commandResult?.Reason == null && commandResult?.Embed == null)
                    {
                        await replyFunc(commandResult?.Reaction?.Name, null);
                    }
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
                        responseMessage = replyFunc($"Sorry, I don't understand that command 😕 use `{_configuration.CommandPrefix}help` for a list of support commands", null);
                        break;

                    case CommandError.ParseFailed:
                        logLevel = LogLevel.Warning;
                        responseMessage = replyFunc($"Sorry, your command contains invalid characters or objects that I can't understand 😕", null);
                        break;

                    case CommandError.BadArgCount:
                        logLevel = LogLevel.Warning;
                        responseMessage = replyFunc($"Sorry, your command has an invalid number of parameters 😕 use `{_configuration.CommandPrefix}help` for details on command usage", null);
                        break;

                    case CommandError.ObjectNotFound:
                        logLevel = LogLevel.Warning;
                        responseMessage = replyFunc($"Sorry, I'm supposed to be able to understand that command, but I can't find the code that should handle it 😅", null);
                        break;

                    case CommandError.MultipleMatches:
                        logLevel = LogLevel.Warning;
                        responseMessage = replyFunc($"Sorry, your command is ambiguous, try be more specific 😕 use `{_configuration.CommandPrefix}help` for details on command usage", null);
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

                if (reactFunc != null)
                {
                    await reactFunc(reactionEmoji);
                }

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
                _ = message.AddReactionSafeAsync(
                    reactions.ElementAt(Random.Shared.Next(reactions.Count))
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to react to message");
            }
        }
    }

    internal class SlashCommandCallback
    {
        public string CommandName { get; set; }

        public SlashCommandProperties Command { get; set; }

        public Type Module { get; set; }

    }
}
