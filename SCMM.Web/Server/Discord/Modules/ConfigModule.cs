using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Data.Models.Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Discord.Modules
{
    [Group("config")]
    public class ConfigModule : ModuleBase<SocketCommandContext>
    {
        private readonly IConfiguration _configuration;
        private readonly ScmmDbContext _db;

        public ConfigModule(IConfiguration configuration, ScmmDbContext db)
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
                .WithName("`>config names`")
                .WithValue("...")
            );
            fields.Add(new EmbedFieldBuilder()
                .WithName("`>config get [name]`")
                .WithValue("...")
            );
            fields.Add(new EmbedFieldBuilder()
                .WithName("`>config set [name] [value]`")
                .WithValue("...")
            );
            fields.Add(new EmbedFieldBuilder()
                .WithName("`>config add [name] [value]`")
                .WithValue("...")
            );
            fields.Add(new EmbedFieldBuilder()
                .WithName("`>config remove [name] [value]`")
                .WithValue("...")
            );
            fields.Add(new EmbedFieldBuilder()
                .WithName("`>config clear [name]`")
                .WithValue("...")
            );
            fields.Add(new EmbedFieldBuilder()
                .WithName("`>config list [name]`")
                .WithValue("...")
            );

            var embed = new EmbedBuilder()
                .WithTitle("Help - Configuration")
                .WithDescription($"...")
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
            fields.Add(new EmbedFieldBuilder()
                .WithName($"{DiscordConfiguration.Currency}")
                .WithValue("...")
            );
            fields.Add(new EmbedFieldBuilder()
                .WithName($"{DiscordConfiguration.Alerts}")
                .WithValue("...")
            );
            fields.Add(new EmbedFieldBuilder()
                .WithName($"{DiscordConfiguration.AlertsChannel}")
                .WithValue("...")
            );

            var embed = new EmbedBuilder()
                .WithTitle("Configuration Names")
                .WithDescription($"...")
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

            var value = guild.Get(name);
            if (!String.IsNullOrEmpty(value))
            {
                await Context.Channel.SendMessageAsync($"'{name}' is '{value}'");
            }
            else
            {
                await Context.Channel.SendMessageAsync($"'{name}' is not set");
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

            var config = guild.Set(name, value);
            _db.SaveChanges();

            if (config != null && !String.IsNullOrEmpty(config.Value))
            {
                await Context.Channel.SendMessageAsync($"Ok. '{name}' is now '{config.Value}'");
            }
            else
            {
                await Context.Channel.SendMessageAsync($"Ok. '{name}' is no longer set");
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

            var values = value?.Split(new[] { ' ', ',', '+', '&', '|', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var config = guild.Add(name, values);
            _db.SaveChanges();

            if (config != null && config.List?.Any() == true)
            {
                await Context.Channel.SendMessageAsync($"Ok. '{name}' is now '{String.Join(", ", config.List)}'");
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

            var values = value?.Split(new[] { ' ', ',', '+', '&', '|', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var config = guild.Remove(name, values);
            _db.SaveChanges();

            if (config != null && config.List?.Any() == true)
            {
                await Context.Channel.SendMessageAsync($"Ok. '{name}' is now '{String.Join(", ", config.List)}'");
            }
            else
            {
                await Context.Channel.SendMessageAsync($"Ok. '{name}' is no longer set");
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

            var config = guild.Clear(name);
            _db.SaveChanges();

            if (config != null)
            {
                await Context.Channel.SendMessageAsync($"Ok. '{name}' is no longer set");
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

            var values = guild.List(name);
            if (values?.Any() == true)
            {
                await Context.Channel.SendMessageAsync($"'{name}' is '{String.Join(", ", values)}'");
            }
            else
            {
                await Context.Channel.SendMessageAsync($"'{name}' is not set");
            }
        }
    }
}
