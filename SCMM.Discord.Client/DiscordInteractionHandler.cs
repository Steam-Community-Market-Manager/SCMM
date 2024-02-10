﻿using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Discord.Client.Commands;
using SCMM.Discord.Client.Extensions;
using System.Reflection;

namespace SCMM.Discord.Client
{
    internal class DiscordInteractionHandler
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _services;
        private readonly InteractionService _interactions;
        private readonly DiscordShardedClient _client;
        private readonly DiscordConfiguration _configuration;

        public DiscordInteractionHandler(ILogger logger, IServiceProvider services, DiscordShardedClient client, DiscordConfiguration configuration)
        {
            var interactionServicesConfig = new InteractionServiceConfig()
            {
                DefaultRunMode = RunMode.Async,
                EnableAutocompleteHandlers = true,
                UseCompiledLambda = true
            };

            _logger = logger;
            _services = services;
            _interactions = new InteractionService(client, interactionServicesConfig);
            _client = client;
            _configuration = configuration;
        }

        public async Task AddInteractionsAsync(params Assembly[] assemblies)
        {
            _client.JoinedGuild += OnJoinedGuildAddInteractionModulesAsync;
            _client.AutocompleteExecuted += OnAutocompleteReceivedAsync;
            _client.SlashCommandExecuted += OnSlashCommandReceivedAsync;
            _interactions.AutocompleteCommandExecuted += OnAutocompleteCommandExecutedAsync;
            _interactions.AutocompleteHandlerExecuted += OnAutocompleteHandlerExecutedAsync;
            _interactions.SlashCommandExecuted += OnSlashCommandExecutedAsync;
            _interactions.Log += OnLogAsync;

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
                        await _interactions.AddModulesAsync(
                            assembly: assembly,
                            services: scope.ServiceProvider
                        );
                    }
                }

                var globalModules = _interactions.Modules.Where(x => x.Preconditions.OfType<RequireContextAttribute>().All(x => x.Contexts != ContextType.Guild)).ToArray();
                await _interactions.AddModulesGloballyAsync(deleteMissing: true, modules: globalModules);

                var guildModules = _interactions.Modules.Where(x => x.Preconditions.OfType<RequireContextAttribute>().Any(x => x.Contexts == ContextType.Guild)).ToArray();
                if (guildModules.Any())
                {
                    foreach (var guild in _client.Guilds)
                    {
                        try
                        {
                            await _interactions.AddModulesToGuildAsync(guild, deleteMissing: true, modules: guildModules);
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message?.Contains("Missing Access") == true)
                            {
                                _logger.LogWarning($"Unable to register guild interaction modules to '{guild.Id}', missing access");
                            }
                            else
                            {
                                _logger.LogError(ex, $"Failed to register guild interaction modules to '{guild.Id}'");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialise interaction modules");
            }
        }

        private Task OnLogAsync(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    _logger.LogError(
                        message.Exception,
                        message.Message
                    );
                    break;

                case LogSeverity.Warning:
                    _logger.LogWarning(
                        message.Exception,
                        message.Message
                    );
                    break;

                case LogSeverity.Info:
                    _logger.LogInformation(
                        message.Message
                    );
                    break;

                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    _logger.LogDebug(
                        message.Message
                    );
                    break;
            }

            return Task.CompletedTask;
        }

        private async Task OnJoinedGuildAddInteractionModulesAsync(SocketGuild guild)
        {
            var guildModules = _interactions.Modules.Where(x => x.Attributes.OfType<RequireContextAttribute>().Any(x => x.Contexts == ContextType.Guild)).ToArray();
            if (guildModules.Any())
            {
                try
                {
                    await _interactions.AddModulesToGuildAsync(guild, deleteMissing: true, modules: guildModules);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to register guild interaction modules");
                }
            }
        }

        private Task OnAutocompleteReceivedAsync(SocketAutocompleteInteraction interaction)
        {
            // Execute in a background thread to avoid clogging the gateway thread
            _ = Task.Run(async () =>
            {
                var context = new ShardedInteractionContext(_client, interaction);
                var result = await _interactions.ExecuteCommandAsync(
                    context: context,
                    services: _services
                );
            });

            return Task.CompletedTask;
        }

        private Task OnAutocompleteCommandExecutedAsync(AutocompleteCommandInfo auto, IInteractionContext context, IResult result)
        {
            // TODO: Logging...
            return Task.CompletedTask;
        }

        private Task OnAutocompleteHandlerExecutedAsync(IAutocompleteHandler autocomplete, IInteractionContext context, IResult result)
        {
            // TODO: Logging...
            return Task.CompletedTask;
        }

        private Task OnSlashCommandReceivedAsync(SocketInteraction cmd)
        {
            return HandleInteractionAsync(cmd);
        }

        private Task OnSlashCommandExecutedAsync(SlashCommandInfo command, IInteractionContext context, IResult result)
        {
            return RespondToInteractionResult(
                ($"{command?.Module?.Name} {command?.Name}").Trim(),
                (context.User.GetFullUsername()),
                (context.Guild != null ? $"{context.Guild.Name} #{context.Guild.Id}" : "n/a"),
                (context.Channel != null ? context.Channel.Name : "n/a"),
                (result),
                (modal) => context.Interaction.RespondWithModalAsync(modal: modal),
                (text, embed, ephemeral) => context.Interaction.FollowupAsync(text: text, embed: embed, ephemeral: ephemeral)
            );
        }

        private Task HandleInteractionAsync(SocketInteraction interaction)
        {
            // Ignore interactions from other robots
            if (interaction.User.IsBot || interaction.User.IsWebhook)
            {
                return Task.CompletedTask;
            }

            // Defer the response to show a "is thinking..." placeholder
            // NOTE: Interactions need to be acknowledged before this callback ends
            Task.WaitAll(
                interaction.DeferAsync()
            );

            // Execute the interaction...
            // NOTE: Interaction service will execute async by default (unless the interaction is configured otherwise)
            var context = new ShardedInteractionContext(_client, interaction);
            Task.WaitAll(
                _interactions.ExecuteCommandAsync(
                    context: context,
                    services: _services
                )
            );

            return Task.CompletedTask;
        }

        private async Task RespondToInteractionResult<T>(string interactionName, string userName, string guildName, string channelName, IResult result, Func<Modal, Task> modalFunc, Func<string, Embed, bool, Task<T>> replyFunc)
            where T : IUserMessage
        {
            if (result == null)
            {
                return;
            }

            var interactionResult = (result as InteractionResult);

            // Success
            if (result.IsSuccess)
            {
                if (interactionResult?.Modal != null)
                {
                    _logger.LogTrace(
                        $"Interaction '{interactionName}' triggered modal prompt (guild: {guildName}, channel: {channelName}, user: {userName})"
                    );
                    await modalFunc(interactionResult.Modal);
                }
                else
                {
                    _logger.LogTrace(
                        $"Interaction '{interactionName}' executed successfully (guild: {guildName}, channel: {channelName}, user: {userName})"
                    );
                    if (interactionResult?.Reason != null || interactionResult?.Embed != null)
                    {
                        await replyFunc(interactionResult.Reason, interactionResult.Embed, interactionResult.Ephemeral);
                    }
                }
            }

            // Error gracefully reported by the interaction handler
            else if (result.Error == InteractionCommandError.Unsuccessful && interactionResult != null)
            {
                _logger.LogTrace(
                    $"Interaction '{interactionName}' had an unsuccessful outcome (guild: {guildName}, channel: {channelName}, user: {userName}). {interactionResult.Reason}"
                );

                if (interactionResult?.Reason != null || interactionResult?.Explaination != null)
                {
                    await replyFunc(
                        (interactionResult.Explaination != null) ? null : interactionResult.Reason,
                        (interactionResult.Explaination == null) ? null : new EmbedBuilder()
                            .WithTitle(interactionResult.Reason)
                            .WithDescription(interactionResult.Explaination)
                            .WithUrl(interactionResult.HelpUrl)
                            .WithImageUrl(interactionResult.HelpImageUrl)
                            .Build(),
                        interactionResult.Ephemeral
                    );
                }
            }

            // Unhandled error thrown by the interaction handler
            else
            {
                var logLevel = LogLevel.Error;
                var responseMessage = Task.CompletedTask;
                switch (result.Error)
                {
                    case InteractionCommandError.UnknownCommand:
                        logLevel = LogLevel.Warning;
                        responseMessage = replyFunc($"Sorry, I don't understand that command 😕", null, true);
                        break;

                    case InteractionCommandError.ParseFailed:
                        logLevel = LogLevel.Warning;
                        responseMessage = replyFunc($"Sorry, I'm supposed to be able to understand that command, but I can't find the code that should handle it anymore 😅", null, true);
                        break;

                    case InteractionCommandError.ConvertFailed:
                        logLevel = LogLevel.Warning;
                        responseMessage = replyFunc($"Sorry, that command contains invalid characters or objects that I can't understand 😅", null, true);
                        break;

                    case InteractionCommandError.BadArgs:
                        logLevel = LogLevel.Warning;
                        responseMessage = replyFunc($"Sorry, that command has an invalid number of parameters 😕. Are you sure you did it correctly?", null, true);
                        break;

                    case InteractionCommandError.UnmetPrecondition:
                        logLevel = LogLevel.Warning;
                        responseMessage = replyFunc($"Sorry, {result.ErrorReason}", null, true);
                        break;

                    case InteractionCommandError.Exception:
                        logLevel = LogLevel.Error;
                        responseMessage = replyFunc($"Sorry, this is embarrassing, but I've encountered an error while processing that command.\nTechnical reason: ```{result.ErrorReason}```", null, true);
                        break;

                    case InteractionCommandError.Unsuccessful:
                        logLevel = LogLevel.Error;
                        responseMessage = replyFunc($"Sorry, that command cannot be completed right, try again later", null, true);
                        break;
                }

                _logger.Log(logLevel,
                    $"Interaction '{interactionName}' failed (guild: {guildName}, channel: {channelName}, user: {userName}). Reason: {result.Error.Value} {result.ErrorReason}"
                );

                await responseMessage;
            }
        }
    }
}
