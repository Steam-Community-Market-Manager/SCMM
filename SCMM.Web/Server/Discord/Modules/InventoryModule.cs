using CommandQuery;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Extensions;
using SCMM.Web.Server.Services;
using SCMM.Web.Server.Services.Commands.FetchAndCreateSteamProfile;
using SCMM.Web.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Discord.Modules
{
    [Group("inventory")]
    public class InventoryModule : ModuleBase<SocketCommandContext>
    {
        private readonly IConfiguration _configuration;
        private readonly ScmmDbContext _db;
        private readonly SteamService _steam;
        private readonly SteamCurrencyService _currencies;
        private readonly ICommandProcessor _commandProcessor;

        public InventoryModule(IConfiguration configuration, ScmmDbContext db, SteamService steam, SteamCurrencyService currencies, ICommandProcessor commandProcessor)
        {
            _configuration = configuration;
            _db = db;
            _steam = steam;
            _currencies = currencies;
            _commandProcessor = commandProcessor;
        }

        /// <summary>
        /// !inventory [steamId] [currencyName]
        /// </summary>
        /// <returns></returns>
        [Command]
        [Alias("value")]
        [Summary("Echoes profile inventory value")]
        public async Task SayProfileInventoryValueAsync(
            [Summary("The SteamID of the profile to check")] string id,
            [Summary("The currency name prices should be displayed as")] string currencyName = null
        )
        {
            var guild = _db.DiscordGuilds
                .Include(x => x.Configurations)
                .FirstOrDefault(x => x.DiscordId == Context.Guild.Id.ToString());
            if (guild == null)
            {
                await ReplyAsync($"Beep boop! I'm unable to find the Discord configuration for this guild");
                return;
            }

            var fetchAndCreateProfile = await _commandProcessor.ProcessWithResultAsync(new FetchAndCreateSteamProfileRequest()
            {
                Id = id
            });

            var profile = fetchAndCreateProfile?.Profile;
            if (profile == null)
            {
                await ReplyAsync($"Beep boop! I'm unable to find that Steam profile (it might be private).\nIf you're using a custom profile name, you can also use your full profile page URL instead");
                return;
            }

            var currency = _currencies.GetByNameOrDefault(currencyName ?? guild.Get(Data.Models.Discord.DiscordConfiguration.Currency).Value);
            if (currency == null)
            {
                await ReplyAsync($"Beep boop! I don't support that currency.");
                return;
            }

            var inventoryTotal = await _steam.GetProfileInventoryTotal(profile.SteamId, currency.Name);
            if (inventoryTotal == null)
            {
                await ReplyAsync($"Beep boop! I'm unable to value that profiles inventory. It's either private, or doesn't contain any items that I monitor.");
                return;
            }

            await Task.Run(async () =>
            {
                var color = Color.Blue;
                var fields = new List<EmbedFieldBuilder>();
                fields.Add(new EmbedFieldBuilder()
                    .WithName("Market Value")
                    .WithValue($"{currency.ToPriceString(inventoryTotal.TotalMarketValue)} {currency.Name}")
                );
                if (inventoryTotal.TotalInvested > 0)
                {
                    /*
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
                    */
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
            });
        }
    }
}
