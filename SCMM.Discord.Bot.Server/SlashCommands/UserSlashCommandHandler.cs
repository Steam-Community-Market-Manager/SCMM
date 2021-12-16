using CommandQuery;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Client;
using SCMM.Discord.Client.Extensions;
using SCMM.Shared.API.Extensions;
using SCMM.Steam.API.Commands;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Data.Store;

namespace SCMM.Discord.Bot.Server.SlashCommands;

//[Group("my")]
//[Summary("Manage your settings")]
public class UserSlashCommandHandler : ISlashCommandModule
{
    private readonly IConfiguration _configuration;
    private readonly SteamDbContext _db;
    private readonly ICommandProcessor _commandProcessor;
    private readonly IQueryProcessor _queryProcessor;

    public UserSlashCommandHandler(IConfiguration configuration, SteamDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
    {
        _configuration = configuration;
        _db = db;
        _commandProcessor = commandProcessor;
        _queryProcessor = queryProcessor;
    }

    [Command("steam")]
    [Summary("Link your SteamID so that you don't have to specify it when using other commands")]
    public async Task<RuntimeResult> SetUserSteamIdAsync(
        [Name("id")][Summary("Valid SteamID or Steam URL")] string steamId,
        SocketSlashCommand cmd = null
    )
    {
        var user = cmd.User;
        var discordId = user.GetFullUsername();

        // Load the profile
        var importedProfile = await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileRequest()
        {
            ProfileId = steamId
        });

        var profile = importedProfile?.Profile;
        if (profile == null)
        {
            return CommandResult.Fail(
                reason: $"Steam profile not found",
                explaination: $"That Steam profile doesn't exist. Supported options are Steam ID64, Custom URL, or Profile URL. You can easily find your Profile URL by viewing your profile in Steam and copying it from the URL bar.",
                helpImageUrl: $"{_configuration.GetWebsiteUrl()}/images/discord/steam_find_your_profile_id.png"
            );
        }

        // Set the discord profile
        profile.DiscordId = discordId;
        await _db.SaveChangesAsync();

        return CommandResult.Success();
    }

    [Command("currency")]
    [Summary("Set your preferred currency so that you don't have to specify it when using other commands")]
    public async Task<RuntimeResult> SetUserCurrencyAsync(
        [Name("code")][Summary("Supported three-letter currency code (e.g. USD, EUR, AUD)")] string currencyId,
        SocketSlashCommand cmd
    )
    {
        var user = cmd.User;
        var discordId = user.GetFullUsername();

        // Load the profile
        var profile = await _db.SteamProfiles
            .FirstOrDefaultAsync(x => x.DiscordId == discordId);

        if (profile == null)
        {
            return CommandResult.Fail(
                reason: $"Steam account not found",
                explaination: $"You need to link your Steam account before you can use this command."
            );
        }

        // Load the currency
        var getCurrencyByName = await _queryProcessor.ProcessAsync(new GetCurrencyByNameRequest()
        {
            Name = currencyId
        });

        var currency = getCurrencyByName.Currency;
        if (currency == null)
        {
            return CommandResult.Fail(
                $"Sorry, I don't support that currency."
            );
        }

        // Set the currency
        profile.Currency = currency;
        await _db.SaveChangesAsync();

        return CommandResult.Success();
    }
}
