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
        var appId = (long) (autocompleteInteraction.Data.Options.FirstOrDefault(x => x.Name == "app")?.Value ?? 0);
        var itemName = autocompleteInteraction.Data.Options.FirstOrDefault(x => x.Name == parameter.Name)?.Value?.ToString();
        using var scope = services.CreateScope();
        {
            var steamDb = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
            var itemAppId = appId > 0 ? appId : (long) _configuration.AppId;
            var itemNames = await steamDb.SteamAssetDescriptions
                .Where(x => x.App.SteamId == itemAppId.ToString())
                .Where(x => x.Name.Contains(itemName))
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
