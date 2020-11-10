using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Extensions;
using SCMM.Web.Server.Services;
using SCMM.Web.Shared.Data.Models.Steam;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Discord.Modules
{
    [Group("trade")]
    public class TradeModule : ModuleBase<SocketCommandContext>
    {
        private readonly IConfiguration _configuration;
        private readonly ScmmDbContext _db;
        private readonly SteamService _steam;

        public TradeModule(IConfiguration configuration, ScmmDbContext db, SteamService steam)
        {
            _configuration = configuration;
            _db = db;
            _steam = steam;
        }

        /// <summary>
        /// !trade help
        /// </summary>
        /// <returns></returns>
        [Command("help")]
        [Summary("Echo module help")]
        public async Task GetModuleHelpAsync()
        {
            var fields = new List<EmbedFieldBuilder>();
            fields.Add(new EmbedFieldBuilder()
                .WithName("`>trade request [steamId]`")
                .WithValue("...")
            );

            var embed = new EmbedBuilder()
                .WithTitle("Help - Trade")
                .WithDescription($"...")
                .WithFields(fields)
                .Build();

            await ReplyAsync(
                embed: embed
            );
        }

        /// <summary>
        /// !trade [steamId]
        /// </summary>
        /// <returns></returns>
        [Command()]
        [Alias("request")]
        [Summary("Echoes profile trade request")]
        public async Task SayProfileTradeRequestAsync(
            [Summary("The SteamID of the profile to request trade for")] string steamId
        )
        {
            var profile = await _steam.AddOrUpdateSteamProfile(steamId, fetchLatest: true);
            if (profile == null)
            {
                await ReplyAsync($"Beep boop! I'm unable to find that Steam profile (it might be private).\nIf you're using a custom profile name, you can also use your full profile page URL instead");
                return;
            }
            if (String.IsNullOrEmpty(profile.TradeUrl))
            {
                await ReplyAsync($"Beep boop! You haven't set your Steam trade URL yet, configure it here: {_configuration.GetBaseUrl()}/settings.");
                return;
            }

            var tradeItems = _db.SteamProfiles
                .Where(x => x.Id == profile.Id)
                .Select(x => new
                {
                    HaveCount = x.InventoryItems.Count(y => y.Flags.HasFlag(SteamProfileInventoryItemFlags.WantToSell) || y.Flags.HasFlag(SteamProfileInventoryItemFlags.WantToTrade)),
                    WantCount = x.MarketItems.Count(y => y.Flags.HasFlag(SteamProfileMarketItemFlags.WantToBuy))
                })
                .FirstOrDefault();

            if (tradeItems.HaveCount <= 0)
            {
                await ReplyAsync($"Beep boop! You haven't set any of your inventory items as tradable yet, manage your inventory here: {_configuration.GetBaseUrl()}/steam/inventory/me.");
                return;
            }

            var description = new StringBuilder();
            if (tradeItems.HaveCount > 0)
            {
                description.AppendLine($"Open to offers on **{tradeItems.HaveCount}** items ([view inventory]({_configuration.GetBaseUrl()}/steam/inventory/{steamId}?tab=0))");
            }
            if (tradeItems.WantCount > 0)
            {
                description.AppendLine($"Looking for **{tradeItems.WantCount}** items ([view wishlist]({_configuration.GetBaseUrl()}/steam/inventory/{steamId}?tab=1))");
            }
            if (description.Length > 0)
            {
                description.AppendLine(String.Empty);
            }
            description.AppendLine($"**[Make a trade offer]({profile.TradeUrl})**");

            await Task.Run(async () =>
            {
                var color = Color.Blue;
                var embed = new EmbedBuilder()
                    .WithTitle($"{profile.Name} is looking for trades")
                    .WithDescription(description.ToString())
                    .WithUrl(!String.IsNullOrEmpty(profile.TradeUrl) ? profile.TradeUrl : $"{_configuration.GetBaseUrl()}/steam/inventory/{profile.SteamId}")
                    .WithImageUrl($"{_configuration.GetBaseUrl()}/api/inventory/{profile.SteamId}/trade/mosaic?timestamp={DateTime.UtcNow.Ticks}")
                    .WithThumbnailUrl(profile.AvatarUrl)
                    .WithColor(color)
                    .WithFooter(x => x.Text = _configuration.GetBaseUrl())
                    .WithCurrentTimestamp()
                    .Build();

                await ReplyAsync(
                    embed: embed
                );
            });
        }
    }
}
