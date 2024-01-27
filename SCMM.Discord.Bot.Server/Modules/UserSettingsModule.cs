using CommandQuery;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Bot.Server.Autocompleters;
using SCMM.Discord.Client.Commands;
using SCMM.Discord.Data.Store;
using SCMM.Shared.API.Extensions;
using SCMM.Steam.API.Commands;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Data.Store;

namespace SCMM.Discord.Bot.Server.Modules;

[Group("config", "User configuration commands")]
public class UserSettingsModule : InteractionModuleBase<ShardedInteractionContext>
{
    private readonly IConfiguration _configuration;
    private readonly DiscordDbContext _discordDb;
    private readonly SteamDbContext _steamDb;
    private readonly ICommandProcessor _commandProcessor;
    private readonly IQueryProcessor _queryProcessor;

    public UserSettingsModule(IConfiguration configuration, DiscordDbContext discordDb, SteamDbContext steamDb, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
    {
        _configuration = configuration;
        _discordDb = discordDb;
        _steamDb = steamDb;
        _commandProcessor = commandProcessor;
        _queryProcessor = queryProcessor;
    }

    private async Task<DiscordUser> GetOrCreateUser()
    {
        var user = await _discordDb.DiscordUsers
            .FirstOrDefaultAsync(x => x.Id == Context.User.Id);

        if (user == null)
        {
            _discordDb.DiscordUsers.Add(
                user = new DiscordUser()
                {
                    Id = Context.User.Id,
                    Username = Context.User.Username,
                    Discriminator = Context.User.Discriminator,
                }
            );
        }

        return user;
    }

    [SlashCommand("steam", "Link your Steam ID so that you don't have to specify it when using other commands")]
    public async Task<RuntimeResult> SetUserSteamIdAsync(
        [Summary("id", "Your SteamID or Steam URL")] string steamId
    )
    {
        var user = await GetOrCreateUser();
        if (user == null)
        {
            return InteractionResult.Fail($"Sorry, I can't find your user record in my database.", ephemeral: true);
        }

        // Find the profile
        var importedProfile = await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileRequest()
        {
            ProfileId = steamId,
            ImportFriendsListAsync = true
        });

        var profile = importedProfile?.Profile;
        if (profile != null)
        {
            await _steamDb.SaveChangesAsync();
        }
        else
        {
            return InteractionResult.Fail(
                reason: $"Steam profile not found",
                explaination: $"That Steam profile doesn't exist. Supported options are Steam ID64, Custom URL, or Profile URL. You can easily find your Profile URL by viewing your profile in Steam and copying it from the URL bar.",
                helpImageUrl: $"{_configuration.GetDataStoreUrl()}/images/discord/steam_find_your_profile_id.png",
                ephemeral: true
            );
        }

        // Associated the profile to the current user
        user.SteamId = importedProfile.Profile.SteamId;
        await _discordDb.SaveChangesAsync();

        return InteractionResult.Success(
            ephemeral: true
        );
    }

    [SlashCommand("currency", "Set your preferred currency so that you don't have to specify it when using other commands")]
    public async Task<RuntimeResult> SetUserCurrencyAsync(
        [Summary("name", "Your preferred three-letter currency name (e.g. USD, EUR, AUD)")][Autocomplete(typeof(CurrencyAutocompleteHandler))] string currencyId
    )
    {
        var user = await GetOrCreateUser();
        if (user == null)
        {
            return InteractionResult.Fail($"Sorry, I can't find your user record in my database.", ephemeral: true);
        }

        // Find the currency
        var getCurrencyByName = await _queryProcessor.ProcessAsync(new GetCurrencyByNameRequest()
        {
            Name = currencyId
        });

        var currency = getCurrencyByName?.Currency;
        if (currency == null)
        {
            return InteractionResult.Fail(
                reason: $"Sorry, I don't support that currency.",
                ephemeral: true
            );
        }

        // Associated the currency to the current user
        user.CurrencyId = currency.Name;
        await _discordDb.SaveChangesAsync();

        return InteractionResult.Success(
            ephemeral: true
        );
    }
}
