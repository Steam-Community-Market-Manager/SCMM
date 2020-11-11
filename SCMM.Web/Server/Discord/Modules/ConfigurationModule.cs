using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _configuration;
        private readonly ScmmDbContext _db;

        private static char[] ValueSeparators = new [] { ' ', ',', '+', '&', '|', ';' };

        public ConfigurationModule(IConfiguration configuration, ScmmDbContext db)
        {
            _configuration = configuration;
            _db = db;
        }

        /// <summary>
        /// !config help
        /// </summary>
        /// <returns></returns>
        [Command]
        [Alias("help")]
        [Summary("Echo config module help")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task GetModuleHelpAsync()
        {
            var fields = new List<EmbedFieldBuilder>();
            fields.Add(new EmbedFieldBuilder()
                .WithName("`config names`")
                .WithValue("Show information about all the supported configurations")
            );
            fields.Add(new EmbedFieldBuilder()
                .WithName("`config get [name]`")
                .WithValue("Get a configuration value")
            );
            fields.Add(new EmbedFieldBuilder()
                .WithName("`config set [name] [value]`")
                .WithValue("Set a configuration value")
            );
            fields.Add(new EmbedFieldBuilder()
                .WithName("`config add [name] [values...]`")
                .WithValue("Add a value to a configuration list")
            );
            fields.Add(new EmbedFieldBuilder()
                .WithName("`config remove [name] [values...]`")
                .WithValue("Remove a value from a configuration list")
            );
            fields.Add(new EmbedFieldBuilder()
                .WithName("`config clear [name]`")
                .WithValue("Clear a configuration list")
            );
            fields.Add(new EmbedFieldBuilder()
                .WithName("`config list [name]`")
                .WithValue("List all the values of a configuration")
            );

            var embed = new EmbedBuilder()
                .WithTitle("Help - Configuration")
                .WithFields(fields)
                .Build();

            await ReplyAsync(
                embed: embed
            );
        }

        /// <summary>
        /// !config names
        /// </summary>
        /// <returns></returns>
        [Command("names")]
        [Summary("Echo configuration names")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
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
                    description.Append($" Allowed options are:");
                    description.AppendLine();
                    description.Append(
                        String.Join(',', definition.AllowedValues.Select(x => $"`{x}`"))
                    );
                }

                fields.Add(new EmbedFieldBuilder()
                    .WithName(definition.Name)
                    .WithValue(description.ToString())
                );
            }

            var embed = new EmbedBuilder()
                .WithTitle("Configuration Names")
                .WithFields(fields)
                .Build();

            await ReplyAsync(
                embed: embed
            );
        }

        /// <summary>
        /// !config get [name]
        /// </summary>
        /// <returns></returns>
        [Command("get")]
        [Summary("Get guild configuration")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task GetConfigValueAsync(
            [Summary("The config name")] string name
        )
        {
            var guild = _db.DiscordGuilds
                .Include(x => x.Configurations)
                .FirstOrDefault(x => x.DiscordId == Context.Guild.Id.ToString());
            if (guild == null)
            {
                await ReplyAsync($"Beep boop! I'm unable to find the Discord configuration for this guild");
                return;
            }

            try
            {
                var config = guild.Get(name);
                if (!String.IsNullOrEmpty(config.Value))
                {
                    await Context.Channel.SendMessageAsync($"'{config.Key?.Name ?? name}' is '{config.Value}'");
                }
                else
                {
                    await Context.Channel.SendMessageAsync($"'{config.Key?.Name ?? name}' is not set");
                }
            }
            catch (ArgumentException ex)
            {
                await Context.Channel.SendMessageAsync(ex.Message);
            }
        }

        /// <summary>
        /// !config set [name] [value]
        /// </summary>
        /// <returns></returns>
        [Command("set")]
        [Summary("Set guild configuration")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task SetConfigValueAsync(
            [Summary("The config name")] string name,
            [Summary("The config value")] string value
        )
        {
            var guild = _db.DiscordGuilds
                .Include(x => x.Configurations)
                .FirstOrDefault(x => x.DiscordId == Context.Guild.Id.ToString());
            if (guild == null)
            {
                await ReplyAsync($"Beep boop! I'm unable to find the Discord configuration for this guild");
                return;
            }

            try
            {
                var config = guild.Set(name, value);
                _db.SaveChanges();

                if (config != null && !String.IsNullOrEmpty(config.Value))
                {
                    await Context.Channel.SendMessageAsync($"Ok. '{config?.Name ?? name}' is now '{config.Value}'");
                }
                else
                {
                    await Context.Channel.SendMessageAsync($"Ok. '{config?.Name ?? name}' is no longer set");
                }
            }
            catch (ArgumentException ex)
            {
                await Context.Channel.SendMessageAsync(ex.Message);
            }
        }

        /// <summary>
        /// !config add [name] [value]
        /// </summary>
        /// <returns></returns>
        [Command("add")]
        [Summary("Add guild configuration")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task AddConfigValueAsync(
            [Summary("The config name")] string name,
            [Summary("The config values")] params string[] values
        )
        {
            var guild = _db.DiscordGuilds
                .Include(x => x.Configurations)
                .FirstOrDefault(x => x.DiscordId == Context.Guild.Id.ToString());
            if (guild == null)
            {
                await ReplyAsync($"Beep boop! I'm unable to find the Discord configuration for this guild");
                return;
            }

            try
            {
                var config = guild.Add(name, values?.SelectMany(x => x.Split(ValueSeparators, StringSplitOptions.RemoveEmptyEntries))?.ToArray());
                _db.SaveChanges();

                if (config != null && config.List?.Any() == true)
                {
                    await Context.Channel.SendMessageAsync($"Ok. '{config?.Name ?? name}' is now '{String.Join(", ", config.List)}'");
                }
            }
            catch (ArgumentException ex)
            {
                await Context.Channel.SendMessageAsync(ex.Message);
            }
        }

        /// <summary>
        /// !config add [name] [value]
        /// </summary>
        /// <returns></returns>
        [Command("remove")]
        [Summary("Remove guild configuration")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task RemoveConfigValueAsync(
            [Summary("The config name")] string name,
            [Summary("The config values")] params string[] values
        )
        {
            var guild = _db.DiscordGuilds
                .Include(x => x.Configurations)
                .FirstOrDefault(x => x.DiscordId == Context.Guild.Id.ToString());
            if (guild == null)
            {
                await ReplyAsync($"Beep boop! I'm unable to find the Discord configuration for this guild");
                return;
            }

            try
            {
                var config = guild.Remove(name, values?.SelectMany(x => x.Split(ValueSeparators, StringSplitOptions.RemoveEmptyEntries))?.ToArray());
                _db.SaveChanges();

                if (config != null && config.List?.Any() == true)
                {
                    await Context.Channel.SendMessageAsync($"Ok. '{config?.Name ?? name}' is now '{String.Join(", ", config.List)}'");
                }
                else
                {
                    await Context.Channel.SendMessageAsync($"Ok. '{config?.Name ?? name}' is no longer set");
                }
            }
            catch (ArgumentException ex)
            {
                await Context.Channel.SendMessageAsync(ex.Message);
            }
        }

        /// <summary>
        /// !config clear [name]
        /// </summary>
        /// <returns></returns>
        [Command("clear")]
        [Summary("Clear guild configuration")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task ClearConfigValueAsync(
            [Summary("The config name")] string name
        )
        {
            var guild = _db.DiscordGuilds
                .Include(x => x.Configurations)
                .FirstOrDefault(x => x.DiscordId == Context.Guild.Id.ToString());
            if (guild == null)
            {
                await ReplyAsync($"Beep boop! I'm unable to find the Discord configuration for this guild");
                return;
            }

            try
            {
                var config = guild.Clear(name);
                _db.SaveChanges();

                if (config != null)
                {
                    await Context.Channel.SendMessageAsync($"Ok. '{config?.Name ?? name}' is no longer set");
                }
            }
            catch (ArgumentException ex)
            {
                await Context.Channel.SendMessageAsync(ex.Message);
            }
        }

        /// <summary>
        /// !config get [name]
        /// </summary>
        /// <returns></returns>
        [Command("list")]
        [Summary("List guild configuration")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task ListConfigValueAsync(
            [Summary("The config name")] string name
        )
        {
            var guild = _db.DiscordGuilds
                .Include(x => x.Configurations)
                .FirstOrDefault(x => x.DiscordId == Context.Guild.Id.ToString());
            if (guild == null)
            {
                await ReplyAsync($"Beep boop! I'm unable to find the Discord configuration for this guild");
                return;
            }

            try
            {
                var config = guild.List(name);
                if (config.Value?.Any() == true)
                {
                    await Context.Channel.SendMessageAsync($"'{config.Key?.Name ?? name}' is '{String.Join(", ", config.Value)}'");
                }
                else
                {
                    await Context.Channel.SendMessageAsync($"'{config.Key?.Name ?? name}' is not set");
                }
            }
            catch (ArgumentException ex)
            {
                await Context.Channel.SendMessageAsync(ex.Message);
            }
        }
    }
}
