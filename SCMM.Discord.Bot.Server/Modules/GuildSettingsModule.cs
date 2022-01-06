using CommandQuery;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Bot.Server.Autocompleters;
using SCMM.Discord.Client.Commands;
using SCMM.Steam.Data.Store;

namespace SCMM.Discord.Bot.Server.Modules;

[RequireContext(ContextType.Guild)]
[RequireUserPermission(GuildPermission.ManageGuild)]
[Group("server", "Server configuration commands")]
public class GuildSettingsModule : InteractionModuleBase<ShardedInteractionContext>
{
    private readonly SteamDbContext _db;
    private readonly ICommandProcessor _commandProcessor;
    private readonly IQueryProcessor _queryProcessor;

    public GuildSettingsModule(SteamDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
    {
        _db = db;
        _commandProcessor = commandProcessor;
        _queryProcessor = queryProcessor;
    }

    private async Task<DiscordGuild> GetOrCreateGuild()
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

    [SlashCommand("get", "Get configuration for this server")]
    public async Task<RuntimeResult> SetGuildConfigurationAsync(
        [Summary("name", "The configuration name")][Autocomplete(typeof(GuildConfigurationNameAutocompleteHandler))] string name
    )
    {
        var guild = await GetOrCreateGuild();
        if (guild == null)
        {
            return InteractionResult.Fail($"Sorry, I can't find this guild in my database.", ephemeral: true);
        }

        try
        {
            var config = guild.Get(name);
            if (!string.IsNullOrEmpty(config.Value))
            {
                return InteractionResult.Success($"{config.Key?.Name ?? name} is {GetValueDisplayText(config.Value)}.", ephemeral: true);
            }
            else
            {
                return InteractionResult.Success($"{config.Key?.Name ?? name} has not been set yet.", ephemeral: true);
            }
        }
        catch (ArgumentException ex)
        {
            return InteractionResult.Fail(ex.Message, ephemeral: true);
        }
    }

    [SlashCommand("set", "Set configuration for this server")]
    public async Task<RuntimeResult> SetGuildConfigurationAsync(
        [Summary("name", "The configuration name")][Autocomplete(typeof(GuildConfigurationNameAutocompleteHandler))] string name,
        [Summary("value", "The configuration value")][Autocomplete(typeof(GuildConfigurationValueAutocompleteHandler))] string value
    )
    {
        var guild = await GetOrCreateGuild();
        if (guild == null)
        {
            return InteractionResult.Fail($"Sorry, I can't find this guild in my database.", ephemeral: true);
        }

        try
        {
            guild.Set(name, value);
            await _db.SaveChangesAsync();

            var config = guild.Get(name);
            if (!string.IsNullOrEmpty(config.Value))
            {
                return InteractionResult.Success($"👌 {config.Key?.Name ?? name} is now {GetValueDisplayText(config.Value)}.", ephemeral: true);
            }
            else
            {
                return InteractionResult.Success($"👌 {config.Key?.Name ?? name} was reset to default.", ephemeral: true);
            }
        }
        catch (ArgumentException ex)
        {
            return InteractionResult.Fail(ex.Message, ephemeral: true);
        }
    }

    private string GetValueDisplayText(string value)
    {
        if (value.StartsWith("<") && value.EndsWith(">"))
        {
            return value;
        }
        else
        {
            return $"`{value}`";
        }
    }
}
