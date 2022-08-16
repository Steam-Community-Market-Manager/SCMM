using CommandQuery;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Bot.Server.Autocompleters;
using SCMM.Discord.Client.Commands;
using SCMM.Discord.Data.Store;

namespace SCMM.Discord.Bot.Server.Modules;

[RequireContext(ContextType.Guild)]
[RequireUserPermission(GuildPermission.ManageGuild)]
[Group("config", "Server configuration commands")]
public class GuildSettingsModule : InteractionModuleBase<ShardedInteractionContext>
{
    private readonly DiscordDbContext _discordDb;
    private readonly ICommandProcessor _commandProcessor;
    private readonly IQueryProcessor _queryProcessor;

    public GuildSettingsModule(DiscordDbContext discordDb, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
    {
        _discordDb = discordDb;
        _commandProcessor = commandProcessor;
        _queryProcessor = queryProcessor;
    }

    private async Task<DiscordGuild> GetOrCreateGuild()
    {
        var guild = await _discordDb.DiscordGuilds
            .FirstOrDefaultAsync(x => x.Id == Context.Guild.Id);

        if (guild == null)
        {
            _discordDb.DiscordGuilds.Add(
                guild = new Discord.Data.Store.DiscordGuild()
                {
                    Id = Context.Guild.Id,
                    Name = Context.Guild.Name
                }
            );
        }

        return guild;
    }

    [SlashCommand("server", "Update the configuration for this server")]
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
            await _discordDb.SaveChangesAsync();

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
