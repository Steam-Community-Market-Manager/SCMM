using CommandQuery;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SCMM.Data.Shared.Extensions;
using SCMM.Discord.Client.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Models.Discord;
using SCMM.Web.Data.Models;
using SCMM.Web.Server.Extensions;
using SCMM.Web.Server.Services;
using SCMM.Web.Server.Services.Commands;
using SCMM.Web.Server.Services.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Discord.Modules
{
    [Group("inventory")]
    [Alias("inv")]
    public class InventoryModule : ModuleBase<SocketCommandContext>
    {
        private readonly IConfiguration _configuration;
        private readonly SteamDbContext _db;
        private readonly SteamService _steamService;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public InventoryModule(IConfiguration configuration, SteamDbContext db, SteamService steam, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
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

            var message = await ReplyAsync("Loading...");
            await message.LoadingAsync("🔍 Finding Steam profile...");

            // Load the profile using the discord id
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

            await SayProfileInventoryValueInternalAsync(message, profile?.SteamId ?? user?.Username, currencyId ?? profile?.Currency?.Name);
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
            // HACK: ">inventory nzd" will trigger this with steamid as nzd rather than currencyId as nzd.
            //       If the steamid is three characters or less, flip them
            if (steamId.Length <= 3)
            {
                await SayProfileInventoryValueAsync(user: null, currencyId: steamId);
            }
            else
            {
                await SayProfileInventoryValueInternalAsync(null, steamId, currencyId);
            }
        }

        private async Task SayProfileInventoryValueInternalAsync(IUserMessage message, string steamId, string currencyId)
        {
            message = (message ?? await ReplyAsync("Loading..."));

            // Load the profile
            await message.LoadingAsync("🔍 Finding Steam profile...");
            var fetchAndCreateProfile = await _commandProcessor.ProcessWithResultAsync(new FetchAndCreateSteamProfileRequest()
            {
                ProfileId = steamId
            });

            var profile = fetchAndCreateProfile?.Profile;
            if (profile == null)
            {
                await message.ModifyAsync(x =>
                    x.Content = $"Beep boop! I'm unable to find that Steam profile (it might be private).\nIf you're using a custom profile name, you can also use your full profile page URL instead."
                );
                return;
            }

            // Load the guild
            var guild = _db.DiscordGuilds
                .AsNoTracking()
                .Include(x => x.Configurations)
                .FirstOrDefault(x => x.DiscordId == this.Context.Guild.Id.ToString());
            if (guild != null && String.IsNullOrEmpty(currencyId))
            {
                currencyId = guild.Get(DiscordConfiguration.Currency).Value;
            }

            // Promote donators from VIP servers to VIP role
            if (guild.Flags.HasFlag(SCMM.Steam.Data.Models.Enums.DiscordGuildFlags.VIP))
            {
                var user = Context.User;
                var roles = Context.Guild.GetUser(user.Id).Roles;
                if (roles.Any(x => x.Name.Contains(Roles.Donator, StringComparison.InvariantCultureIgnoreCase)))
                {
                    var discordId = $"{user.Username}#{user.Discriminator}";
                    if (String.Equals(profile.DiscordId, discordId, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (!profile.Roles.Any(x => x == Roles.VIP))
                        {
                            profile.Roles.Add(Roles.VIP);
                        }
                    }
                }
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
            await message.LoadingAsync("🔄 Fetching inventory details from Steam...");
            _ = await _commandProcessor.ProcessWithResultAsync(new FetchSteamProfileInventoryRequest()
            {
                ProfileId = profile.Id.ToString()
            });

            _db.SaveChanges();

            // Calculate the profiles inventory totals
            await message.LoadingAsync("💱 Calculating inventory value...");
            var inventoryTotals = await _queryProcessor.ProcessAsync(new GetSteamProfileInventoryTotalsRequest()
            {
                ProfileId = profile.SteamId,
                CurrencyId = currency.SteamId,
            });
            if (inventoryTotals == null)
            {
                await message.ModifyAsync(x =>
                    x.Content = $"Beep boop! I'm unable to value that profiles inventory. It's either private, or doesn't contain any items that I monitor."
                );
                return;
            }

            // Snapshot the profiles inventory totals
            if (profile.LastSnapshotInventoryOn == null || profile.LastSnapshotInventoryOn <= DateTime.Now.Subtract(TimeSpan.FromHours(1)))
            {
                await message.LoadingAsync("💾 Snapshotting inventory value...");
                await _commandProcessor.ProcessAsync(new SnapshotSteamProfileInventoryValueRequest()
                {
                    ProfileId = profile.SteamId,
                    CurrencyId = currency.SteamId,
                    InventoryTotals = inventoryTotals
                });

                _db.SaveChanges();
            }

            // Generate the profiles inventory thumbnail
            await message.LoadingAsync("🎨 Generating inventory thumbnail...");
            var inventoryThumbnail = await _commandProcessor.ProcessWithResultAsync(new GenerateSteamProfileInventoryThumbnailRequest()
            {
                ProfileId = profile.SteamId,
                ExpiresOn = DateTimeOffset.Now.AddDays(7)
            });

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
                .WithName("📈 Current Market Value")
                .WithValue(currency.ToPriceString(inventoryTotals.TotalMarketValue))
                .WithIsInline(false)
            );
            if (inventoryTotals.TotalInvested != null && inventoryTotals.TotalInvested > 0)
            {
                fields.Add(new EmbedFieldBuilder()
                    .WithName("💰 Invested")
                    .WithValue(currency.ToPriceString(inventoryTotals.TotalInvested.Value))
                    .WithIsInline(true)
                );
                fields.Add(new EmbedFieldBuilder()
                    .WithName((inventoryTotals.TotalResellProfit >= 0) ? "⬆️ Profit" : "⬇️ Loss")
                    .WithValue(currency.ToPriceString(inventoryTotals.TotalResellProfit))
                    .WithIsInline(true)
                );
            }

            var embed = new EmbedBuilder()
                .WithAuthor(author)
                .WithDescription($"Inventory of {inventoryTotals.TotalItems.ToQuantityString()} item(s).")
                .WithFields(fields)
                .WithImageUrl($"{_configuration.GetBaseUrl()}/api/image/{inventoryThumbnail?.Image?.Id}")
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
