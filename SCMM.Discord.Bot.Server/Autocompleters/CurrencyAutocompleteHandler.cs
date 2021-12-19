using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SCMM.Steam.Data.Store;

namespace SCMM.Discord.Bot.Server.Autocompleters;

public class CurrencyAutocompleteHandler : AutocompleteHandler
{
    public async override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        var value = autocompleteInteraction.Data.Options.FirstOrDefault(x => x.Name == parameter.Name)?.Value?.ToString();
        using var scope = services.CreateScope();
        {
            var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
            var currencies = await db.SteamCurrencies
                .Where(x => x.Name.Contains(value))
                .Select(x => new AutocompleteResult()
                {
                    Name = x.Name,
                    Value = x.Name
                })
                .OrderBy(x => x.Name)
                .Take(25)
                .ToListAsync();

            return AutocompletionResult.FromSuccess(
                currencies
            );
        }
    }
}
