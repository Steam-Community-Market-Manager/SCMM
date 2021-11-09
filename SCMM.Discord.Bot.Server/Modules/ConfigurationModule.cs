using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Client;
using SCMM.Steam.Data.Store;
using System.Text;
using DiscordConfiguration = SCMM.Steam.Data.Store.DiscordConfiguration;

namespace SCMM.Discord.Bot.Server.Modules
{
    [Group("configuration")]
    [Alias("config", "cfg")]
    [RequireContext(ContextType.Guild)]
    public class ConfigurationModule : ModuleBase<SocketCommandContext>
    {
        private readonly SteamDbContext _db;

        private static readonly char[] ValueSeparators = new[] { ' ', ',', '+', '&', '|', ';' };

        public ConfigurationModule(SteamDbContext db)
        {
            _db = db;
        }

        private async Task<Steam.Data.Store.DiscordGuild> GetOrCreateGuild()
        {
            var guild = await _db.DiscordGuilds
                .Include(x => x.Configurations)
                .FirstOrDefaultAsync(x => x.DiscordId == Context.Guild.Id.ToString());

            if (guild == null)
            {
                _db.DiscordGuilds.Add(guild = new Steam.Data.Store.DiscordGuild()
                {
                    DiscordId = Context.Guild.Id.ToString(),
                    Name = Context.Guild.Name
                });
            }

            return guild;
        }

        [Command("options")]
        [Alias("names")]
        [Summary("Show a list of all supported configuration options you can personalise for this server")]
        public RuntimeResult GetConfigNames()
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
                        string.Join(',', definition.AllowedValues.Select(x => $"`{x}`"))
                    );
                }

                fields.Add(new EmbedFieldBuilder()
                    .WithName($"{definition.Name}: `{definition.Name.ToLower()}`")
                    .WithValue(description.ToString())
                );
            }

            var embed = new EmbedBuilder()
                .WithTitle("Configuration Options")
                .WithDescription("The following configuration names can be used with the `config x` commands. Use the `help` command for more information.")
                .WithFields(fields)
                .Build();

            return CommandResult.Success(
                embed: embed
            );
        }

        [Command("get")]
        [Summary("Get the value of a configuration for this server")]
        [Remarks("You must be a server manager to use this command")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task<RuntimeResult> GetConfigValueAsync(
            [Name("option")][Summary("Supported config option name")] string option
        )
        {
            var guild = await GetOrCreateGuild();
            if (guild == null)
            {
                return CommandResult.Fail($"Beep boop! I'm unable to find the Discord configuration for this guild.");
            }

            try
            {
                var config = guild.Get(option);
                if (!string.IsNullOrEmpty(config.Value))
                {
                    return CommandResult.Success($"'{config.Key?.Name ?? option}' is '{config.Value}'");
                }
                else
                {
                    return CommandResult.Success($"'{config.Key?.Name ?? option}' is not set");
                }
            }
            catch (ArgumentException ex)
            {
                return CommandResult.Fail(ex.Message);
            }
        }

        [Command("set")]
        [Summary("Set the value of a configuration for this server")]
        [Remarks("You must be a server manager to use this command")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task<RuntimeResult> SetConfigValueAsync(
            [Name("option")][Summary("Supported config option name")] string option,
            [Name("value")][Summary("New value to set as the config")][Remainder] string value
        )
        {
            var guild = await GetOrCreateGuild();
            if (guild == null)
            {
                return CommandResult.Fail($"Beep boop! I'm unable to find the Discord configuration for this guild.");
            }

            try
            {
                var config = guild.Set(option, value);
                await _db.SaveChangesAsync();

                if (config != null && !string.IsNullOrEmpty(config.Value))
                {
                    return CommandResult.Success($"Ok. '{config?.Name ?? option}' is now '{config.Value}'.");
                }
                else
                {
                    return CommandResult.Success($"Ok. '{config?.Name ?? option}' is no longer set.");
                }
            }
            catch (ArgumentException ex)
            {
                return CommandResult.Fail(ex.Message);
            }
        }

        [Command("add")]
        [Summary("Add the specified value to a configuration for this server")]
        [Remarks("You must be a server manager to use this command")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task<RuntimeResult> AddConfigValueAsync(
            [Name("option")][Summary("Supported config option name")] string option,
            [Name("values")][Summary("New value to add to the config")] params string[] values
        )
        {
            var guild = await GetOrCreateGuild();
            if (guild == null)
            {
                return CommandResult.Fail($"Beep boop! I'm unable to find the Discord configuration for this guild.");
            }

            try
            {
                var config = guild.Add(option, values?.SelectMany(x => x.Split(ValueSeparators, StringSplitOptions.RemoveEmptyEntries))?.ToArray());
                await _db.SaveChangesAsync();

                return CommandResult.Success($"Ok. '{config?.Name ?? option}' is now '{string.Join(", ", config.List)}'.");
            }
            catch (ArgumentException ex)
            {
                return CommandResult.Fail(ex.Message);
            }
        }

        [Command("remove")]
        [Summary("Remove the specified value from a configuration for this server")]
        [Remarks("You must be a server manager to use this command")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task<RuntimeResult> RemoveConfigValueAsync(
            [Name("option")][Summary("Supported config option name")] string option,
            [Name("values")][Summary("Value to remove from the config")] params string[] values
        )
        {
            var guild = await GetOrCreateGuild();
            if (guild == null)
            {
                return CommandResult.Fail($"Beep boop! I'm unable to find the Discord configuration for this guild.");
            }

            try
            {
                var config = guild.Remove(option, values?.SelectMany(x => x.Split(ValueSeparators, StringSplitOptions.RemoveEmptyEntries))?.ToArray());
                await _db.SaveChangesAsync();

                if (config != null && config.List?.Any() == true)
                {
                    return CommandResult.Success($"Ok. '{config?.Name ?? option}' is now '{string.Join(", ", config.List)}'.");
                }
                else
                {
                    return CommandResult.Success($"Ok. '{config?.Name ?? option}' is no longer set.");
                }
            }
            catch (ArgumentException ex)
            {
                return CommandResult.Fail(ex.Message);
            }
        }

        [Command("clear")]
        [Summary("Clear all values from a configuration for this server")]
        [Remarks("You must be a server manager to use this command")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task<RuntimeResult> ClearConfigValueAsync(
            [Name("option")][Summary("Supported config option name")] string option
        )
        {
            var guild = await GetOrCreateGuild();
            if (guild == null)
            {
                return CommandResult.Fail($"Beep boop! I'm unable to find the Discord configuration for this guild.");
            }

            try
            {
                var config = guild.Clear(option);
                await _db.SaveChangesAsync();

                return CommandResult.Success($"Ok. '{config?.Name ?? option}' is no longer set.");
            }
            catch (ArgumentException ex)
            {
                return CommandResult.Fail(ex.Message);
            }
        }

        [Command("list")]
        [Summary("List the set values for a configuration for this server")]
        [Remarks("You must be a server manager to use this command")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task<RuntimeResult> ListConfigValueAsync(
            [Name("option")][Summary("Supported config option name")] string option
        )
        {
            var guild = await GetOrCreateGuild();
            if (guild == null)
            {
                return CommandResult.Fail($"Beep boop! I'm unable to find the Discord configuration for this guild.");
            }

            try
            {
                var config = guild.List(option);
                if (config.Value?.Any() == true)
                {
                    return CommandResult.Success($"'{config.Key?.Name ?? option}' is '{string.Join(", ", config.Value)}'.");
                }
                else
                {
                    return CommandResult.Success($"'{config.Key?.Name ?? option}' is not set.");
                }
            }
            catch (ArgumentException ex)
            {
                return CommandResult.Fail(ex.Message);
            }
        }
    }
}
