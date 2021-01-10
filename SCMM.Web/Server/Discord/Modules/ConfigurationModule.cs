using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Data.Models.Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Discord.Modules
{
    [Group("config")]
    public class ConfigurationModule : ModuleBase<SocketCommandContext>
    {
        private readonly ScmmDbContext _db;

        private static char[] ValueSeparators = new [] { ' ', ',', '+', '&', '|', ';' };

        public ConfigurationModule(ScmmDbContext db)
        {
            _db = db;
        }

        [Command("options")]
        [Summary("Show a list of all supported configuration options you can personalise for this server")]
        public async Task GetConfigNamesAsync()
        {
            var fields = new List<EmbedFieldBuilder>();
            var configurationDefinitions = DiscordConfiguration.Definitions;
            foreach (var definition in configurationDefinitions)
            {
                var description = new StringBuilder();
                description.Append(definition.Description);
                if (definition.AllowedValues?.Length > 0)
                {
                    description.Append($" Accepted values are:");
                    description.AppendLine();
                    description.Append(
                        String.Join(',', definition.AllowedValues.Select(x => $"`{x}`"))
                    );
                }

                fields.Add(new EmbedFieldBuilder()
                    .WithName($"definition.Name: `{definition.Name.ToLower()}`")
                    .WithValue(description.ToString())
                );
            }

            var embed = new EmbedBuilder()
                .WithTitle("Configuration Options")
                .WithDescription("The following configuration names can be used with the `config x` commands. Use the `help` command for more information.")
                .WithFields(fields)
                .Build();

            await ReplyAsync(
                embed: embed
            );
        }

        [Command("get")]
        [Summary("Get the value of a configuration for this server")]
        [Remarks("You must be a server manager to use this command")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task GetConfigValueAsync(
            [Name("option")][Summary("Supported config option name")] string option
        )
        {
            var guild = _db.DiscordGuilds
                .AsNoTracking()
                .Include(x => x.Configurations)
                .FirstOrDefault(x => x.DiscordId == Context.Guild.Id.ToString());
            if (guild == null)
            {
                await ReplyAsync($"Beep boop! I'm unable to find the Discord configuration for this guild.");
                return;
            }

            try
            {
                var config = guild.Get(option);
                if (!String.IsNullOrEmpty(config.Value))
                {
                    await ReplyAsync($"'{config.Key?.Name ?? option}' is '{config.Value}'");
                }
                else
                {
                    await ReplyAsync($"'{config.Key?.Name ?? option}' is not set");
                }
            }
            catch (ArgumentException ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        [Command("set")]
        [Summary("Set the value of a configuration for this server")]
        [Remarks("You must be a server manager to use this command")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task SetConfigValueAsync(
            [Name("option")][Summary("Supported config option name")] string option,
            [Name("value")][Summary("New value to set as the config")][Remainder] string value
        )
        {
            var guild = _db.DiscordGuilds
                .AsNoTracking()
                .Include(x => x.Configurations)
                .FirstOrDefault(x => x.DiscordId == Context.Guild.Id.ToString());
            if (guild == null)
            {
                await ReplyAsync($"Beep boop! I'm unable to find the Discord configuration for this guild.");
                return;
            }

            try
            {
                var config = guild.Set(option, value);
                _db.SaveChanges();

                if (config != null && !String.IsNullOrEmpty(config.Value))
                {
                    await ReplyAsync($"Ok. '{config?.Name ?? option}' is now '{config.Value}'.");
                }
                else
                {
                    await ReplyAsync($"Ok. '{config?.Name ?? option}' is no longer set.");
                }
            }
            catch (ArgumentException ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        [Command("add")]
        [Summary("Add the specified value to a configuration for this server")]
        [Remarks("You must be a server manager to use this command")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task AddConfigValueAsync(
            [Name("option")][Summary("Supported config option name")] string option,
            [Name("values")][Summary("New value to add to the config")][Remainder] params string[] values
        )
        {
            var guild = _db.DiscordGuilds
                .AsNoTracking()
                .Include(x => x.Configurations)
                .FirstOrDefault(x => x.DiscordId == Context.Guild.Id.ToString());
            if (guild == null)
            {
                await ReplyAsync($"Beep boop! I'm unable to find the Discord configuration for this guild.");
                return;
            }

            try
            {
                var config = guild.Add(option, values?.SelectMany(x => x.Split(ValueSeparators, StringSplitOptions.RemoveEmptyEntries))?.ToArray());
                _db.SaveChanges();

                if (config != null && config.List?.Any() == true)
                {
                    await ReplyAsync($"Ok. '{config?.Name ?? option}' is now '{String.Join(", ", config.List)}'.");
                }
            }
            catch (ArgumentException ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        [Command("remove")]
        [Summary("Remove the specified value from a configuration for this server")]
        [Remarks("You must be a server manager to use this command")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task RemoveConfigValueAsync(
            [Name("option")][Summary("Supported config option name")] string option,
            [Name("values")][Summary("Value to remove from the config")][Remainder] params string[] values
        )
        {
            var guild = _db.DiscordGuilds
                .AsNoTracking()
                .Include(x => x.Configurations)
                .FirstOrDefault(x => x.DiscordId == Context.Guild.Id.ToString());
            if (guild == null)
            {
                await ReplyAsync($"Beep boop! I'm unable to find the Discord configuration for this guild.");
                return;
            }

            try
            {
                var config = guild.Remove(option, values?.SelectMany(x => x.Split(ValueSeparators, StringSplitOptions.RemoveEmptyEntries))?.ToArray());
                _db.SaveChanges();

                if (config != null && config.List?.Any() == true)
                {
                    await ReplyAsync($"Ok. '{config?.Name ?? option}' is now '{String.Join(", ", config.List)}'.");
                }
                else
                {
                    await ReplyAsync($"Ok. '{config?.Name ?? option}' is no longer set.");
                }
            }
            catch (ArgumentException ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        [Command("clear")]
        [Summary("Clear all values from a configuration for this server")]
        [Remarks("You must be a server manager to use this command")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task ClearConfigValueAsync(
            [Name("option")][Summary("Supported config option name")] string option
        )
        {
            var guild = _db.DiscordGuilds
                .AsNoTracking()
                .Include(x => x.Configurations)
                .FirstOrDefault(x => x.DiscordId == Context.Guild.Id.ToString());
            if (guild == null)
            {
                await ReplyAsync($"Beep boop! I'm unable to find the Discord configuration for this guild.");
                return;
            }

            try
            {
                var config = guild.Clear(option);
                _db.SaveChanges();

                if (config != null)
                {
                    await ReplyAsync($"Ok. '{config?.Name ?? option}' is no longer set.");
                }
            }
            catch (ArgumentException ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        [Command("list")]
        [Summary("List the set values for a configuration for this server")]
        [Remarks("You must be a server manager to use this command")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task ListConfigValueAsync(
            [Name("option")][Summary("Supported config option name")] string option
        )
        {
            var guild = _db.DiscordGuilds
                .AsNoTracking()
                .Include(x => x.Configurations)
                .FirstOrDefault(x => x.DiscordId == Context.Guild.Id.ToString());
            if (guild == null)
            {
                await ReplyAsync($"Beep boop! I'm unable to find the Discord configuration for this guild.");
                return;
            }

            try
            {
                var config = guild.List(option);
                if (config.Value?.Any() == true)
                {
                    await ReplyAsync($"'{config.Key?.Name ?? option}' is '{String.Join(", ", config.Value)}'.");
                }
                else
                {
                    await ReplyAsync($"'{config.Key?.Name ?? option}' is not set.");
                }
            }
            catch (ArgumentException ex)
            {
                await ReplyAsync(ex.Message);
            }
        }
    }
}
