using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using DiscordConfiguration = SCMM.Discord.Client.DiscordConfiguration;

namespace SCMM.Discord.Bot.Server.Autocompleters;

public class StoreNameAutocompleteHandler : AutocompleteHandler
{
    private readonly DiscordConfiguration _configuration;

    public StoreNameAutocompleteHandler(DiscordConfiguration discordConfiguration)
    {
        _configuration = discordConfiguration;
    }

    public async override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        char[] trimCharacters = { ' ', '-' };
        var value = autocompleteInteraction.Data.Options.FirstOrDefault(x => x.Name == parameter.Name)?.Value?.ToString();
        using var scope = services.CreateScope();
        {
            var appId = _configuration.AppId;
            var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
            var itemNames = await db.SteamItemStores
                .Where(x => x.App.SteamId == appId.ToString())
                .Where(x => 
                    (String.IsNullOrEmpty(value)) ||
                    (!String.IsNullOrEmpty(x.Name) && x.Name.Contains(value)) ||
                    (x.Start != null && (value.Contains(x.Start.Value.Year.ToString())))
                )
                .OrderByDescending(x => x.Start == null)
                .ThenByDescending(x => x.Start)
                .Select(x => new AutocompleteResult()
                {
                    Name = $"{(x.Start != null ? x.Start.Value.ToString("yyyy MMMM d") : null)}{(x.Start != null ? x.Start.Value.GetDaySuffix() : null)} - {x.Name}".Trim(trimCharacters),
                    Value = x.Id.ToString()
                })
                .Take(25)
                .ToListAsync();

            return AutocompletionResult.FromSuccess(
                itemNames
            );
        }
    }
}
