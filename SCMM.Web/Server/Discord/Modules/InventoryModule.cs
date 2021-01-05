using CommandQuery;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using SCMM.Web.Server.Extensions;
using SCMM.Web.Server.Services;
using SCMM.Web.Server.Services.Commands;
using SCMM.Web.Server.Services.Queries;
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
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public InventoryModule(IConfiguration configuration, SteamService steam, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _configuration = configuration;
            _steam = steam;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
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

            var getCurrencyByName = await _queryProcessor.ProcessAsync(new GetCurrencyByNameRequest()
            {
                Name = currencyName
            });

            var currency = getCurrencyByName.Currency;
            if (currency == null)
            {
                await ReplyAsync($"Beep boop! I don't support that currency.");
                return;
            }

            var inventoryTotal = await _steam.GetProfileInventoryTotal(profile, currency);
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
