using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Data.Store;

namespace SCMM.Discord.Bot.Server.Autocompleters;

public class GuildConfigurationNameAutocompleteHandler : AutocompleteHandler
{
    public async override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        using var scope = services.CreateScope();
        {
            var discordDb = scope.ServiceProvider.GetRequiredService<DiscordDbContext>();
            var guild = await discordDb.DiscordGuilds.FirstOrDefaultAsync(x => x.Id == context.Guild.Id);
            var name = autocompleteInteraction.Data.Options.FirstOrDefault(x => x.Name == parameter.Name)?.Value?.ToString();
            var configNames = DiscordGuild.GuildConfiguration.Definitions
                .Where(x => x.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                .Where(x => x.RequiredFlags == 0 || (guild != null && (((int) guild.Flags) & x.RequiredFlags) > 0))
                .Select(x => new AutocompleteResult()
                {
                    Name = x.Name,
                    Value = x.Name
                })
                .OrderBy(x => x.Name)
                .Take(25)
                .ToList();

            return AutocompletionResult.FromSuccess(
                configNames
            );
        }

    }
}
