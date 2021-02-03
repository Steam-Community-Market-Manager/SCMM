using CommandQuery;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Data.Models;
using SCMM.Web.Server.Extensions;
using SCMM.Web.Server.Services.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Discord.Modules
{
    [Group("badge")]
    [Alias("badges")]
    public class BadgeModule : ModuleBase<SocketCommandContext>
    {
        private readonly IConfiguration _configuration;
        private readonly ScmmDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public BadgeModule(IConfiguration configuration, ScmmDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _configuration = configuration;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
        }

        [Command("add")]
        [Summary("Add a new badge for this server that can be assigned to users")]
        [Remarks("You must be a server manager to use this command")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task AddBadgeDefinitionAsync(
            [Name("name")][Summary("Name of the badge")] string name,
            [Name("iconUrl")][Summary("An image that represents the badge")][Remainder] string iconUrl
        )
        {
            var guild = _db.DiscordGuilds
                .Include(x => x.BadgeDefinitions)
                .FirstOrDefault(x => x.DiscordId == Context.Guild.Id.ToString());
            if (guild == null)
            {
                await ReplyAsync($"Beep boop! I'm unable to find the Discord configuration for this guild.");
                return;
            }

            try
            {
                var badgeIcon = await _commandProcessor.ProcessWithResultAsync(new FetchAndCreateImageDataRequest()
                {
                    Url = iconUrl
                });

                guild.BadgeDefinitions.Add(
                    new DiscordBadgeDefinition()
                    {
                        Name = name,
                        Icon = badgeIcon.Image
                    }
                );

                _db.SaveChanges();

                await ReplyAsync($"Ok. '{name}' can now be assigned as a badge.");
            }
            catch (ArgumentException ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        [Command("remove")]
        [Summary("Remove a badge from this server")]
        [Remarks("You must be a server manager to use this command")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task RemoveBadgeDefinitionAsync(
            [Name("name")][Summary("Name of the badge")] string name
        )
        {
            var guild = _db.DiscordGuilds
                .Include(x => x.BadgeDefinitions)
                .FirstOrDefault(x => x.DiscordId == Context.Guild.Id.ToString());
            if (guild == null)
            {
                await ReplyAsync($"Beep boop! I'm unable to find the Discord configuration for this guild.");
                return;
            }

            try
            {
                var badgeDefinition = guild.BadgeDefinitions.FirstOrDefault(x => x.Name == name);
                if (badgeDefinition == null)
                {
                    await ReplyAsync($"Beep boop! I'm unable to find a badge named '{name}' for this guild.");
                    return;
                }

                guild.BadgeDefinitions.Remove(badgeDefinition);

                _db.SaveChanges();

                await ReplyAsync($"Ok. '{name}' is no longer a badge.");
            }
            catch (ArgumentException ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        [Command("options")]
        [Summary("List all defined badges for this server that can be assigned to users")]
        public async Task ListBadgeDefinitionsAsync()
        {
            var guild = _db.DiscordGuilds
                .AsNoTracking()
                .FirstOrDefault(x => x.DiscordId == Context.Guild.Id.ToString());
            if (guild == null)
            {
                await ReplyAsync($"Beep boop! I'm unable to find the Discord configuration for this guild.");
                return;
            }

            try
            {
                var embed = new EmbedBuilder()
                    .WithImageUrl($"{_configuration.GetBaseUrl()}/api/discord/{Context.Guild.Id}/badgeMosaic")
                    .WithFooter(x => x.Text = _configuration.GetBaseUrl())
                    .Build();

                await ReplyAsync(
                    embed: embed
                );
            }
            catch (ArgumentException ex)
            {
                await ReplyAsync(ex.Message);
            }
        }
    }
}
