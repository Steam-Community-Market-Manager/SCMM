using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using SCMM.Web.Server.Extensions;
using SCMM.Web.Server.Services;
using SCMM.Web.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Discord.Modules
{
    [Group("inventory")]
    public class InventoryModule : ModuleBase<SocketCommandContext>
    {
        private readonly IConfiguration _configuration;
        private readonly SteamService _steam;
        private readonly SteamCurrencyService _currencies;

        public InventoryModule(IConfiguration configuration, SteamService steam, SteamCurrencyService currencies)
        {
            _configuration = configuration;
            _steam = steam;
            _currencies = currencies;
        }

        /// <summary>
        ///  !inventory [steamId] [currencyName]
        /// </summary>
        /// <returns></returns>
        [Command()]
        [Alias("value")]
        [Summary("Echoes profile inventory value")]
        public async Task SayProfileInventoryValueAsync(
            [Summary("The SteamID of the profile to check")] string steamId,
            [Summary("The currency name prices should be displayed as")] string currencyName = null
        )
        {
            var profile = await _steam.AddOrUpdateSteamProfile(steamId, fetchLatest: true);
            if (profile == null)
            {
                await ReplyAsync($"Beep boop! Unable to find Steam profile \"{steamId}\". Make sure the SteamID is correct and that the profile is set to public.");
                return;
            }

            var currency = _currencies.GetByNameOrDefault(currencyName);
            if (currency == null)
            {
                await ReplyAsync($"Beep boop! \"{currencyName}\" is not a supported currency.");
                return;
            }

            var inventoryTotal = _steam.GetProfileInventoryTotal(steamId, currency.Name);
            if (inventoryTotal == null)
            {
                await ReplyAsync($"Beep boop! Unable to access inventory for \"{steamId}\", their profile privacy is probably set to private.");
                return;
            }

            var color = Color.Blue;
            var fields = new List<EmbedFieldBuilder>();
            fields.Add(new EmbedFieldBuilder()
                .WithName("Market Value")
                .WithValue($"{currency.ToPriceString(inventoryTotal.TotalMarketValue)} {currency.Name}")
            );
            if (inventoryTotal.TotalInvested > 0)
            {
                var profitLoss = String.Empty;
                var profitLossPrefix = String.Empty;
                if (inventoryTotal.TotalResellProfit >= 0)
                {
                    profitLoss = "Profit";
                    profitLossPrefix = "🡱";
                    color = Color.Green;
                }
                else
                {
                    profitLoss = "Loss";
                    profitLossPrefix = "🡳";
                    color = Color.Red;
                }
                fields.Add(new EmbedFieldBuilder()
                    .WithName("Invested")
                    .WithValue($"{currency.ToPriceString(inventoryTotal.TotalInvested)} {currency.Name}")
                );
                fields.Add(new EmbedFieldBuilder()
                    .WithName(profitLoss)
                    .WithValue($"{profitLossPrefix} {currency.ToPriceString(inventoryTotal.TotalResellProfit)} {currency.Name}")
                );
            }

            var embed = new EmbedBuilder()
                .WithTitle(profile.Name)
                .WithDescription($"Inventory of {inventoryTotal.TotalItems.ToQuantityString()} item(s).")
                .WithFields(fields)
                .WithUrl($"{_configuration.GetBaseUrl()}/steam/inventory/{profile.SteamId}")
                .WithImageUrl($"{_configuration.GetBaseUrl()}/api/inventory/{profile.SteamId}/mosaic?rows=3&columns=5&timestamp={DateTime.UtcNow.Ticks}")
                .WithThumbnailUrl(profile.AvatarUrl)
                .WithColor(color)
                .WithFooter(x => x.Text = _configuration.GetBaseUrl())
                .WithCurrentTimestamp()
                .Build();

            await ReplyAsync(
                embed: embed
            );
        }
    }
}
