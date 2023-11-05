using CommandQuery;
using Discord;
using Discord.Commands;
using SCMM.Discord.Client.Commands;
using SCMM.Discord.Data.Store;
using SCMM.Shared.Abstractions.Analytics;
using SCMM.Shared.Abstractions.Finance;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Abstractions.Statistics;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Store;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace SCMM.Discord.Bot.Server.Modules
{
    [RequireOwner]
    [RequireContext(ContextType.DM)]
    [Group("administration")]
    [Alias("admin")]
    public partial class AdministrationModule : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordDbContext _discordDb;
        private readonly SteamConfiguration _steamConfiguration;
        private readonly SteamDbContext _steamDb;
        private readonly SteamWebApiClient _steamWebApiClient;
        private readonly SteamCommunityWebClient _steamCommunityClient;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IServiceBus _serviceBus;
        private readonly ICurrencyExchangeService _currencyExchangeService;
        private readonly ITimeSeriesAnalysisService _timeSeriesAnalysisService;
        private readonly IStatisticsService _statisticsService;
        private readonly CommandService _commandService;

        public AdministrationModule(DiscordDbContext discordDb, SteamConfiguration steamConfiguration, SteamDbContext steamDb, SteamWebApiClient steamWebApiClient, SteamCommunityWebClient steamCommunityClient, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IServiceBus serviceBus, ICurrencyExchangeService currencyExchangeService, ITimeSeriesAnalysisService timeSeriesAnalysisService, IStatisticsService statisticsService, CommandService commandService)
        {
            _discordDb = discordDb;
            _steamConfiguration = steamConfiguration;
            _steamDb = steamDb;
            _steamWebApiClient = steamWebApiClient;
            _steamCommunityClient = steamCommunityClient;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _serviceBus = serviceBus;
            _currencyExchangeService = currencyExchangeService;
            _timeSeriesAnalysisService = timeSeriesAnalysisService;
            _statisticsService = statisticsService;
            _commandService = commandService;
        }

        [Command]
        [Priority(0)]
        [Name("help")]
        [Alias("help", "?")]
        [Summary("Show this command help text")]
        public RuntimeResult ShowHelp()
        {
            var embed = new EmbedBuilder();
            var modules = _commandService.Modules
                .Where(x => x.Group == GetType().GetCustomAttribute<GroupAttribute>().Prefix)
                .OrderBy(x => x.Group)
                .ToList();

            foreach (var module in modules)
            {
                var commands = module.Commands.OrderBy(x => x.Name);
                foreach (var command in commands)
                {
                    // Build the command parameter info
                    var commandParamSummaries = new List<string>();
                    var commandParamDetails = new List<string>();
                    if (command.Parameters.Any())
                    {
                        foreach (var param in command.Parameters)
                        {
                            var paramSummary = param.Name.ToLower();
                            if (param.IsOptional)
                            {
                                paramSummary = $"?:{paramSummary}";
                            }
                            if (param.IsMultiple)
                            {
                                paramSummary = $"{paramSummary}...";
                            }
                            commandParamSummaries.Add($"{{{paramSummary}}}");

                            var paramDetail = new StringBuilder();
                            paramDetail.Append($"**{param.Name.ToLower()}**:");
                            if (param.IsOptional)
                            {
                                paramDetail.Append($" Optional,");
                            }
                            if (!string.IsNullOrEmpty(param.Summary))
                            {
                                paramDetail.Append($" {param.Summary}.");
                            }
                            if (param.IsMultiple)
                            {
                                paramDetail.Append($" Multiple values are accepted.");
                            }
                            if (param.DefaultValue != null)
                            {
                                paramDetail.Append($" Default value is \"{param.DefaultValue}\".");
                            }
                            commandParamDetails.Add($"- {paramDetail}");
                        }
                    }

                    // Build the command summary info
                    var commandSummary = new StringBuilder();
                    commandSummary.Append("`");
                    if (!string.IsNullOrEmpty(module.Group))
                    {
                        commandSummary.Append($"{module.Group.ToLower()} ");
                    }
                    if (!string.IsNullOrEmpty(command.Name))
                    {
                        commandSummary.Append($"{command.Name.ToLower()}");
                    }
                    if (commandParamSummaries.Any())
                    {
                        commandSummary.Append(" ");
                        commandSummary.Append(string.Join(' ', commandParamSummaries));
                    }
                    commandSummary.Append("`");

                    // Build the command detailed info
                    var commandDescription = new StringBuilder();
                    if (!string.IsNullOrEmpty(command.Summary))
                    {
                        commandDescription.Append(command.Summary);
                    }
                    if (!string.IsNullOrEmpty(command.Remarks))
                    {
                        if (commandDescription.Length > 0)
                        {
                            commandDescription.Append(". ");
                        }
                        commandDescription.Append(command.Remarks);
                    }
                    if (commandDescription.Length == 0)
                    {
                        commandDescription.Append("No information available");
                    }
                    if (!commandDescription.ToString().EndsWith("."))
                    {
                        commandDescription.Append(".");
                    }
                    if (commandParamDetails.Any())
                    {
                        commandDescription.AppendLine();
                        commandDescription.AppendLine(string.Join('\n', commandParamDetails));
                    }

                    // Add the command to the list
                    embed.AddField(
                        commandSummary.ToString(),
                        commandDescription.ToString()
                    );
                }
            }

            return CommandResult.Success(
                embed: embed.Build()
            );
        }
    }
}
