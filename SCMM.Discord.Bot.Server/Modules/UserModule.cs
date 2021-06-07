using CommandQuery;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SCMM.Discord.Client;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Web.Extensions;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Client.Exceptions;
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
        [Alias("steam")]
        [Summary("Link your SteamID to your Discord user so that you don't have to specify it when using other commands")]
        public async Task<RuntimeResult> SetUserSteamIdAsync(
            [Name("steam_id")][Summary("Valid SteamID or Steam profile URL")] string steamId
        )
        {
            var user = Context.User;
            var discordId = $"{user.Username}#{user.Discriminator}";
           
            // Load the profile
            var importedProfile = await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileRequest()
            {
                ProfileId = steamId
            });

            var profile = importedProfile?.Profile;
            if (profile == null)
            {
                return CommandResult.Fail(
                    reason: $"Steam ID could not be found",
                    explaination: $"That Steam ID doesn't exist. You can find your ID by viewing your Steam profile and copying the unique name or number shown in the URL bar. Pasting the full URL also works.",
                    helpImageUrl: $"{_configuration.GetWebsiteUrl()}/images/discord/steam_find_your_profile_id.png"
                );
            }

            // Set the discord profile
            profile.DiscordId = discordId;

            // Load the discord guild
            var guild = await _db.DiscordGuilds
                .AsNoTracking()
                .Include(x => x.Configurations)
                .FirstOrDefaultAsync(x => x.DiscordId == this.Context.Guild.Id.ToString());

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
                        }
                    }
                }
            }

            await _db.SaveChangesAsync();

            return CommandResult.Success();
        }
    }
}
