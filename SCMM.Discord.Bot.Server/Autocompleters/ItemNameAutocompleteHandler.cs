using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SCMM.Steam.Data.Store;
using DiscordConfiguration = SCMM.Discord.Client.DiscordConfiguration;

namespace SCMM.Discord.Bot.Server.Autocompleters;

public class ItemNameAutocompleteHandler : AutocompleteHandler
{
    private readonly DiscordConfiguration _configuration;

    public ItemNameAutocompleteHandler(DiscordConfiguration discordConfiguration)
    {
        _configuration = discordConfiguration;
    }

    public async override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        var value = autocompleteInteraction.Data.Options.FirstOrDefault(x => x.Name == parameter.Name)?.Value?.ToString();
        using var scope = services.CreateScope();
        {
            var appId = _configuration.AppId;
            var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
            var itemNames = await db.SteamAssetDescriptions
                .Where(x => x.App.SteamId == appId.ToString())
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
                itemNames
            );
        }
    }
}
