using CommandQuery;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SCMM.Discord.Client;
using SCMM.Discord.Client.Extensions;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Web.Extensions;
using SCMM.Steam.API.Commands;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Data.Store;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Discord.Bot.Server.Modules
{
    public class UserModule : ModuleBase<SocketCommandContext>
    {
        private readonly IConfiguration _configuration;
        private readonly SteamDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public UserModule(IConfiguration configuration, SteamDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _configuration = configuration;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
        }

        [Command("steamid")]
        [Alias("steam", "steamId64", "customUrl", "profileUrl")]
        [Summary("Link your SteamID so that you don't have to specify it when using other commands")]
        public async Task<RuntimeResult> SetUserSteamIdAsync(
            [Name("steam_id")][Summary("Valid SteamID or Steam URL")] string steamId
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
                return CommandResult.Fail(
                    reason: $"Steam profile not found",
                    explaination: $"That Steam profile doesn't exist. Supported options are Steam ID64, Custom URL, or Profile URL. You can easily find your Profile URL by viewing your profile in Steam and copying it from the URL bar.",
                    helpImageUrl: $"{_configuration.GetWebsiteUrl()}/images/discord/steam_find_your_profile_id.png"
                );
            }

            // Set the discord profile
            profile.DiscordId = discordId;

            if (Context.Guild != null)
            {
                // Load the discord guild
                var guild = await _db.DiscordGuilds
                    .AsNoTracking()
                    .Include(x => x.Configurations)
                    .FirstOrDefaultAsync(x => x.DiscordId == Context.Guild.Id.ToString());

                // Promote donators from VIP servers to VIP role
                if (guild?.Flags.HasFlag(SCMM.Steam.Data.Models.Enums.DiscordGuildFlags.VIP) == true)
                {
                    var roles = Context.Guild.GetUser(user.Id).Roles;
                    if (roles.Any(x => x.Name.Contains(Roles.Donator, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        if (String.Equals(profile.DiscordId, discordId, StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (!profile.Roles.Any(x => x == Roles.VIP))
                            {
                                profile.Roles.Add(Roles.VIP);
                                await Context.Message.AddReactionsAsync(new IEmote[]
                                {
                                Emote.Parse(":regional_indicator_v:"), Emote.Parse(":regional_indicator_i:"), Emote.Parse(":regional_indicator_p:"), new Emoji("🎁") // "VIP gift"
                                });
                            }
                        }
                    }
                }
            }

            await _db.SaveChangesAsync();

            return CommandResult.Success();
        }

        [Command("currency")]
        [Summary("Set your preferred currency so that you don't have to specify it when using other commands")]
        public async Task<RuntimeResult> SetUserCurrencyAsync(
            [Name("currency_id")][Summary("Supported three-letter currency code (e.g. USD, EUR, AUD)")] string currencyId = null
        )
        {
            var user = Context.User;
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
}
