using CommandQuery;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Extensions;
using SCMM.Web.Server.Services.Queries;
using SCMM.Web.Shared.Data.Models.Steam;
using SCMM.Web.Shared.Domain;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Discord.Modules
{
    /* TODO: Finish this...
    [Group("trade")]
    public class TradeModule : ModuleBase<SocketCommandContext>
    {
        private readonly IConfiguration _configuration;
        private readonly ScmmDbContext _db;
        private readonly IQueryProcessor _queryProcessor;

        public TradeModule(IConfiguration configuration, ScmmDbContext db, IQueryProcessor queryProcessor)
        {
            _configuration = configuration;
            _db = db;
            _queryProcessor = queryProcessor;
        }

        [Command]
        [Priority(1)]
        [Name("request")]
        [Alias("request")]
        [Summary("Show trading information for a Discord user. This only works if the user has a profile on the SCMM website and has linked their Discord account and if the profile has set their Steam trade URL on their profile and has flagged some 'wanted' and 'trading' items on their inventory")]
        public async Task SayProfileTradeRequestAsync(
            [Name("discord_user")][Summary("Valid Discord user name or user mention")] SocketUser user = null
        )
        {
            user = (user ?? Context.User);

            // Load the profile using the discord id
            var message = await ReplyAsync("Finding Steam profile...");
            var discordId = $"{user.Username}#{user.Discriminator}";
            var profile = _db.SteamProfiles
                .AsNoTracking()
                .Include(x => x.Currency)
                .FirstOrDefault(x => x.DiscordId == discordId);
            
            //if (profile == null)
            //{
            //    await message.ModifyAsync(x =>
            //        x.Content = $"Beep boop! I'm unable to find that Discord user. {user.Username} has not linked their Discord and Steam accounts yet, configure it here: {_configuration.GetBaseUrl()}/settings."
            //    );
            //    return;
            //}

    await SayProfileTradeRequestInternalAsync(message, profile?.SteamId ?? user?.Username);
        }

        [Command]
        [Priority(0)]
        [Name("request")]
        [Alias("request")]
        [Summary("Show trading information for a Steam profile. Only works if the profile has set their Steam trade URL on their profile and has flagged some 'wanted' and 'trading' items on their inventory")]
        public async Task SayProfileTradeRequestAsync(
            [Name("steam_id")][Summary("Valid SteamID or Steam profile URL")] string steamId
        )
        {
            await SayProfileTradeRequestInternalAsync(null, steamId);
        }

        private async Task SayProfileTradeRequestInternalAsync(IUserMessage message, string steamId)
        {
            message = (message ?? await ReplyAsync("Finding Steam profile..."));
            var resolvedId = await _queryProcessor.ProcessAsync(new ResolveSteamIdRequest()
            {
                Id = steamId
            });

            var profile = _db.SteamProfiles
                .AsNoTracking()
                .FirstOrDefault(x => x.Id == resolvedId.Id);
            if (profile == null)
            {
                await message.ModifyAsync(x =>
                    x.Content = $"Beep boop! I'm unable to find that Steam profile (it might be private).\nIf you're using a custom profile name, you can also use your full profile page URL instead."
                );
                return;
            }

            if (String.IsNullOrEmpty(profile.TradeUrl))
            {
                await message.ModifyAsync(x =>
                    x.Content = $"Beep boop! You haven't set your Steam trade URL yet, configure it here: {_configuration.GetBaseUrl()}/settings."
                );
                return;
            }

            await message.ModifyAsync(x =>
                x.Content = "Calculating items for trade..."
            );
            var tradeItems = _db.SteamProfiles
                .AsNoTracking()
                .Where(x => x.Id == profile.Id)
                .Select(x => new
                {
                    HaveCount = x.InventoryItems.Count(y => y.Flags.HasFlag(SteamProfileInventoryItemFlags.WantToSell) || y.Flags.HasFlag(SteamProfileInventoryItemFlags.WantToTrade)),
                    WantCount = x.MarketItems.Count(y => y.Flags.HasFlag(SteamProfileMarketItemFlags.WantToBuy))
                })
                .FirstOrDefault();

            if (tradeItems.HaveCount <= 0)
            {
                await message.ModifyAsync(x =>
                    x.Content = $"Beep boop! You haven't marked any of your inventory items as tradable yet, manage your inventory here: {_configuration.GetBaseUrl()}/steam/inventory/{steamId}."
                );
                return;
            }
            if (tradeItems.WantCount <= 0)
            {
                await message.ModifyAsync(x =>
                    x.Content = $"Beep boop! You haven't added any items to your wishlist yet, manage your wishlist here: {_configuration.GetBaseUrl()}/steam/inventory/{steamId}."
                );
                return;
            }

            var description = new StringBuilder();
            if (tradeItems.HaveCount > 0)
            {
                description.AppendLine($"Open to offers on **{tradeItems.HaveCount}** items ([view inventory]({_configuration.GetBaseUrl()}/steam/inventory/{profile.SteamId}?tab=0))");
            }
            if (tradeItems.WantCount > 0)
            {
                description.AppendLine($"Looking for **{tradeItems.WantCount}** items ([view wishlist]({_configuration.GetBaseUrl()}/steam/inventory/{profile.SteamId}?tab=1))");
            }
            if (description.Length > 0)
            {
                description.AppendLine();
            }
            description.AppendLine($"**[Make a trade offer]({profile.TradeUrl})**");

            var color = Color.Blue;
            if (profile.Roles.Contains(Roles.VIP))
            {
                color = Color.Gold;
            }

            var author = new EmbedAuthorBuilder()
                .WithName($"{profile.Name} is looking for trades")
                .WithIconUrl(profile.AvatarUrl)
                .WithUrl(!String.IsNullOrEmpty(profile.TradeUrl) ? profile.TradeUrl : $"{_configuration.GetBaseUrl()}/steam/inventory/{profile.SteamId}");

            var embed = new EmbedBuilder()
                .WithAuthor(author)
                .WithDescription(description.ToString())
                .WithImageUrl($"{_configuration.GetBaseUrl()}/api/profile/{profile.SteamId}/trade/mosaic?timestamp={DateTime.UtcNow.Ticks}")
                .WithThumbnailUrl(profile.AvatarUrl)
                .WithColor(color)
                .WithFooter(x => x.Text = _configuration.GetBaseUrl())
                .Build();

            await message.DeleteAsync();
            await ReplyAsync(
                embed: embed
            );
        }
    }
    */
}
