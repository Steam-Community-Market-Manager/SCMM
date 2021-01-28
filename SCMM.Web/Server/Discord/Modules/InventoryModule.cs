using CommandQuery;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Data.Models.Discord;
using SCMM.Web.Server.Extensions;
using SCMM.Web.Server.Services;
using SCMM.Web.Server.Services.Commands;
using SCMM.Web.Server.Services.Queries;
using SCMM.Web.Shared;
using SCMM.Web.Shared.Domain;
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
        private readonly SteamService _steamService;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public InventoryModule(IConfiguration configuration, ScmmDbContext db, SteamService steam, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _configuration = configuration;
            _db = db;
            _steamService = steam;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
        }
        
        [Command]
        [Priority(1)]
        [Name("value")]
        [Alias("value")]
        [Summary("Show the current inventory market value for a Discord user. This only works if the user has a profile on the SCMM website and has linked their Discord account.")]
        public async Task SayProfileInventoryValueAsync(
            [Name("discord_user")][Summary("Valid Discord user name or user mention")] SocketUser user = null,
            [Name("currency_id")][Summary("Supported three-letter currency code (e.g. USD, EUR, AUD)")] string currencyId = null
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
            /*
            if (profile == null)
            {
                await message.ModifyAsync(x =>
                    x.Content = $"Beep boop! I'm unable to find that Discord user. {user.Username} has not linked their Discord and Steam accounts yet, configure it here: {_configuration.GetBaseUrl()}/settings."
                );
                return;
            }
            */

            await SayProfileInventoryValueInternalAsync(message, profile?.SteamId ?? user.Username, currencyId ?? profile.Currency?.Name);
        }
        
        [Command]
        [Priority(0)]
        [Name("value")]
        [Alias("value")]
        [Summary("Show the current inventory market value for a Steam profile. This only works if the profile and the inventory privacy is set as public.")]
        public async Task SayProfileInventoryValueAsync(
            [Name("steam_id")][Summary("Valid SteamID or Steam profile URL")] string steamId,
            [Name("currency_id")][Summary("Supported three-letter currency code (e.g. USD, EUR, AUD)")] string currencyId = null
        )
        {
            await SayProfileInventoryValueInternalAsync(null, steamId, currencyId);
        }

        private async Task SayProfileInventoryValueInternalAsync(IUserMessage message, string steamId, string currencyId)
        {
            // Load the profile
            message = (message ?? await ReplyAsync("Finding Steam profile..."));
            var fetchAndCreateProfile = await _commandProcessor.ProcessWithResultAsync(new FetchAndCreateSteamProfileRequest()
            {
                Id = steamId
            });

            var profile = fetchAndCreateProfile?.Profile;
            if (profile == null)
            {
                await message.ModifyAsync(x =>
                    x.Content = $"Beep boop! I'm unable to find that Steam profile (it might be private).\nIf you're using a custom profile name, you can also use your full profile page URL instead."
                );
                return;
            }

            var guild = _db.DiscordGuilds
                .AsNoTracking()
                .Include(x => x.Configurations)
                .FirstOrDefault(x => x.DiscordId == this.Context.Guild.Id.ToString());
            if (guild != null && String.IsNullOrEmpty(currencyId))
            {
                currencyId = guild.Get(DiscordConfiguration.Currency).Value;
            }

            // Load the currency
            var getCurrencyByName = await _queryProcessor.ProcessAsync(new GetCurrencyByNameRequest()
            {
                Name = currencyId 
            });

            var currency = getCurrencyByName.Currency;
            if (currency == null)
            {
                await message.ModifyAsync(x =>
                    x.Content = $"Beep boop! I don't support that currency."
                );
                return;
            }

            // Reload the profiles inventory
            await message.ModifyAsync(x =>
                x.Content = "Fetching inventory details from Steam..."
            );
            await _commandProcessor.ProcessAsync(new FetchSteamProfileInventoryRequest()
            {
                Id = profile.Id
            });

            _db.SaveChanges();

            // Calculate the profiles inventory totals
            await message.ModifyAsync(x => 
                x.Content = "Calculating inventory value..."
            );
            var inventoryTotal = await _steamService.GetProfileInventoryTotal(profile.SteamId, currency.SteamId);
            if (inventoryTotal == null)
            {
                await message.ModifyAsync(x =>
                    x.Content = $"Beep boop! I'm unable to value that profiles inventory. It's either private, or doesn't contain any items that I monitor."
                );
                return;
            }

            _db.SaveChanges();

            var color = Color.Blue;
            if (profile.Roles.Contains(Roles.VIP))
            {
                color = Color.Gold;
            }

            var author = new EmbedAuthorBuilder()
                .WithName(profile.Name)
                .WithIconUrl(profile.AvatarUrl)
                .WithUrl($"{_configuration.GetBaseUrl()}/steam/inventory/{profile.SteamId}");

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
                .WithAuthor(author)
                .WithDescription($"Inventory of {inventoryTotal.TotalItems.ToQuantityString()} item(s).")
                .WithFields(fields)
                .WithImageUrl($"{_configuration.GetBaseUrl()}/api/inventory/{profile.SteamId}/mosaic?rows=4&columns=4&timestamp={DateTime.UtcNow.Ticks}")
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
}
