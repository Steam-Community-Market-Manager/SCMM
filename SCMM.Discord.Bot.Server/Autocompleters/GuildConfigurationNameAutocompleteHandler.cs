using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SCMM.Steam.Data.Store;

namespace SCMM.Discord.Bot.Server.Autocompleters;

public class GuildConfigurationNameAutocompleteHandler : AutocompleteHandler
{
    public async override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        var name = autocompleteInteraction.Data.Options.FirstOrDefault(x => x.Name == parameter.Name)?.Value?.ToString();
        var configNames = DiscordConfiguration.Definitions
            .Where(x => x.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
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
