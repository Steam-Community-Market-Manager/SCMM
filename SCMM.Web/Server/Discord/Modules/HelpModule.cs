using Discord;
using Discord.Commands;
using SCMM.Discord.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Discord.Modules
{
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        private readonly DiscordConfiguration _cfg;
        private readonly CommandService _commandService;

        public HelpModule(DiscordConfiguration cfg, CommandService commandService)
        {
            _cfg = cfg;
            _commandService = commandService;
        }

        [Command("help")]
        [Summary("Show this command help text")]
        public async Task SayHelpAsync()
        {
            var embed = new EmbedBuilder();
            var modules = _commandService.Modules.ToList();
            foreach (var module in modules)
            {
                var commands = module.Commands;
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
                                paramDetail.Append($" *Optional,*");
                            }
                            if (!string.IsNullOrEmpty(param.Summary))
                            {
                                paramDetail.Append($" *{param.Summary}.*");
                            }
                            if (param.IsMultiple)
                            {
                                paramDetail.Append($" *Multiple values are accepted.*");
                            }
                            if (param.DefaultValue != null)
                            {
                                paramDetail.Append($" *Default value is \"{param.DefaultValue}\".*");
                            }
                            commandParamDetails.Add($"- {paramDetail}");
                        }
                    }

                    // Build the command summary info
                    var commandSummary = new StringBuilder();
                    commandSummary.Append("`");
                    commandSummary.Append(_cfg.CommandPrefix);
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

            await ReplyAsync(
                message: "These are all the commands I support.",
                embed: embed.Build()
            );
        }
    }
}
