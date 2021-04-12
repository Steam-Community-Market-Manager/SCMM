using CommandQuery;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SCMM.Web.Data.Models;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Services;
using SCMM.Web.Server.Services.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Discord.Modules
{
    public class UserModule : ModuleBase<SocketCommandContext>
    {
        private readonly IConfiguration _configuration;
        private readonly ScmmDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public UserModule(IConfiguration configuration, ScmmDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _configuration = configuration;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
        }

        [Command("steamid")]
        [Alias("steam")]
        [Summary("Link your SteamID to your Discord user so that you don't have to specify it when using other commands")]
        public async Task SetUserSteamIdAsync(
            [Name("steam_id")][Summary("Valid SteamID or Steam profile URL")] string steamId
        )
        {
            var user = Context.User;
            var discordId = $"{user.Username}#{user.Discriminator}";

            // Load the steam profile
            var fetchAndCreateProfile = await _commandProcessor.ProcessWithResultAsync(new FetchAndCreateSteamProfileRequest()
            {
                ProfileId = steamId
            });

            var profile = fetchAndCreateProfile?.Profile;
            if (profile == null)
            {
                await Context.Message.AddReactionAsync(
                   new Emoji("❌")
                );
                return;
            }

            // Set the discord profile
            profile.DiscordId = discordId;

            // Load the discord guild
            var guild = _db.DiscordGuilds
                .AsNoTracking()
                .Include(x => x.Configurations)
                .FirstOrDefault(x => x.DiscordId == this.Context.Guild.Id.ToString());

            // Promote donators from VIP servers to VIP role
            if (guild?.Flags.HasFlag(SCMM.Web.Data.Models.Discord.DiscordGuildFlags.VIP) == true)
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

            _db.SaveChanges();

            await Context.Message.AddReactionAsync(
               new Emoji("✅")
            );
        }
    }
}
