using CommandQuery;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Bot.Server.Autocompleters;
using SCMM.Discord.Client.Attributes;
using SCMM.Discord.Client.Commands;
using SCMM.Discord.Client.Extensions;
using SCMM.Shared.API.Extensions;
using SCMM.Shared.Data.Models;
using SCMM.Steam.API.Commands;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Data.Store;

namespace SCMM.Discord.Bot.Server.Modules;

[Global]
[Group("config", "User configuration commands")]
public class UserSettingsModule : InteractionModuleBase<ShardedInteractionContext>
{
    private readonly IConfiguration _configuration;
    private readonly SteamDbContext _db;
    private readonly ICommandProcessor _commandProcessor;
    private readonly IQueryProcessor _queryProcessor;

    public UserSettingsModule(IConfiguration configuration, SteamDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
    {
        _configuration = configuration;
        _db = db;
        _commandProcessor = commandProcessor;
        _queryProcessor = queryProcessor;
    }

    [SlashCommand("steam", "Link your Steam ID so that you don't have to specify it when using other commands")]
    public async Task<RuntimeResult> SetUserSteamIdAsync(
        [Summary("id", "Your SteamID or Steam URL")] string steamId
    )
    {
        var user = Context.User;
        var discordId = user.GetFullUsername();

        // Load the profile
        var importedProfile = await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileRequest()
        {
            ProfileId = steamId
        });

        var profile = importedProfile?.Profile;
        if (profile == null)
        {
            return InteractionResult.Fail(
                reason: $"Steam profile not found",
                explaination: $"That Steam profile doesn't exist. Supported options are Steam ID64, Custom URL, or Profile URL. You can easily find your Profile URL by viewing your profile in Steam and copying it from the URL bar.",
                helpImageUrl: $"{_configuration.GetWebsiteUrl()}/images/discord/steam_find_your_profile_id.png",
                ephemeral: true
            );
        }

        // Set the discord profile
        profile.DiscordId = discordId;

        var message = (string)null;
        if (Context.Guild != null)
        {
            // Load the discord guild
            var guild = await _db.DiscordGuilds
                .AsNoTracking()
                .Include(x => x.Configurations)
                .FirstOrDefaultAsync(x => x.DiscordId == Context.Guild.Id.ToString());

            // Promote donators from VIP servers to VIP role
            if (guild?.Flags.HasFlag(Steam.Data.Models.Enums.DiscordGuildFlags.VIP) == true)
            {
                var guildRoles = Context.Guild.Roles.ToList();
                var guildUser = await Context.Client.Rest.GetGuildUserAsync(Context.Guild.Id, Context.User.Id);
                var guildUserRoles = guildRoles.Where(x => guildUser?.RoleIds?.Contains(x.Id) == true).ToList();
                if (guildUserRoles?.Any(x => x.Name.Contains(Roles.Donator, StringComparison.InvariantCultureIgnoreCase)) == true)
                {
                    if (string.Equals(profile.DiscordId, discordId, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (!profile.Roles.Any(x => x == Roles.VIP))
                        {
                            profile.Roles.Add(Roles.VIP);
                            message = $"👌 🎁 Thank you for contributing to the SCMM project and/or the {Context.Guild.Name} community. You've been assigned the {Roles.VIP} role as a small token of our appreciation.";
                        }
                    }
                }
            }
        }

        await _db.SaveChangesAsync();

        return InteractionResult.Success(
            message: message,
            ephemeral: true
        );
    }

    [SlashCommand("currency", "Set your preferred currency so that you don't have to specify it when using other commands")]
    public async Task<RuntimeResult> SetUserCurrencyAsync(
        [Summary("name", "Your preferred three-letter currency name (e.g. USD, EUR, AUD)")][Autocomplete(typeof(CurrencyAutocompleteHandler))] string currencyId
    )
    {
        var user = Context.User;
        var discordId = user.GetFullUsername();

        // Load the profile
        var profile = await _db.SteamProfiles
            .FirstOrDefaultAsync(x => x.DiscordId == discordId);

        if (profile == null)
        {
            return InteractionResult.Fail(
                reason: $"Steam account not found",
                explaination: $"You need to link your Steam account before you can use this command.",
                ephemeral: true
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
            return InteractionResult.Fail(
                reason: $"Sorry, I don't support that currency.",
                ephemeral: true
            );
        }

        // Set the currency
        profile.Currency = currency;
        await _db.SaveChangesAsync();

        return InteractionResult.Success(
            ephemeral: true
        );
    }
}
