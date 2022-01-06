using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SCMM.Steam.Data.Store;

namespace SCMM.Discord.Bot.Server.Autocompleters;

public class GuildConfigurationValueAutocompleteHandler : AutocompleteHandler
{
    public async override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        // Auto-complete from known values
        var names = autocompleteInteraction.Data.Options.Where(x => x.Name != parameter.Name).Select(x => x.Value?.ToString()).Where(x => !String.IsNullOrEmpty(x)).ToArray();
        var value = autocompleteInteraction.Data.Options.FirstOrDefault(x => x.Name == parameter.Name)?.Value?.ToString();
        var configValues = DiscordConfiguration.Definitions
            .Where(x => names.Contains(x.Name))
            .Where(x => x.AllowedValues != null)
            .SelectMany(x => x.AllowedValues)
            .Where(x => x.Contains(value, StringComparison.OrdinalIgnoreCase))
            .Select(x => new AutocompleteResult()
            {
                Name = x,
                Value = x
            })
            .OrderBy(x => x.Name)
            .Take(25)
            .ToList();

        // Auto-complete from channel list
        if (names.Any(x => x.Contains("Channel")) && !configValues.Any())
        {
            var channels = await context.Guild.GetTextChannelsAsync();
            configValues = channels
                .Select(x => new AutocompleteResult()
                {
                    Name = x.Name,
                    Value = $"<#{x.Id}>"
                })
                .OrderBy(x => x.Name)
                .Take(25)
                .ToList();
        }

        return AutocompletionResult.FromSuccess(
            configValues
        );
    }
}
