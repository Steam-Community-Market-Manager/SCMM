using CommandQuery;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SCMM.Discord.Client;
using SCMM.Discord.Client.Extensions;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Web.Extensions;
using SCMM.Steam.API;
using SCMM.Steam.API.Commands;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Data.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiscordConfiguration = SCMM.Steam.Data.Store.DiscordConfiguration;

namespace SCMM.Discord.Bot.Server.Modules
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
        public async Task<RuntimeResult> SayProfileInventoryValueAsync(
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

            return await SayProfileInventoryValueInternalAsync(message, profile?.SteamId ?? user?.Username, currencyId ?? profile?.Currency?.Name);
        }

        [Command]
        [Priority(0)]
        [Name("value")]
        [Alias("value")]
        [Summary("Show the current inventory market value for a Steam profile. This only works if the profile and the inventory privacy is set as public.")]
        public async Task<RuntimeResult> SayProfileInventoryValueAsync(
            [Name("steam_id")][Summary("Valid SteamID or Steam profile URL")] string steamId,
            [Name("currency_id")][Summary("Supported three-letter currency code (e.g. USD, EUR, AUD)")] string currencyId = null
        )
        {
            // HACK: ">inventory nzd" will trigger this with steamid as nzd rather than currencyId as nzd.
            //       If the steamid is three characters or less, flip them
            if (steamId.Length <= 3)
            {
                return await SayProfileInventoryValueAsync(user: null, currencyId: steamId);
            }
            else
            {
                return await  SayProfileInventoryValueInternalAsync(null, steamId, currencyId);
            }
        }

        private async Task<RuntimeResult> SayProfileInventoryValueInternalAsync(IUserMessage message, string steamId, string currencyId)
        {
            message = (message ?? await ReplyAsync("Loading..."));
            
            var profile = (SteamProfile)null;
            try
            {
                // Load the profile
                await message.LoadingAsync("🔍 Finding Steam profile...");
                var fetchAndCreateProfile = await _commandProcessor.ProcessWithResultAsync(new FetchAndCreateSteamProfileRequest()
                {
                    ProfileId = steamId
                });

                profile = fetchAndCreateProfile?.Profile;
            }
            catch (SteamRequestException ex)
            {
                if (ex.Error?.Message?.Contains("profile could not be found", StringComparison.InvariantCultureIgnoreCase) == true)
                {
                    await message.DeleteAsync();
                    return CommandResult.Fail(
                        reason: $"Steam ID could not be found",
                        explaination: $"That Steam ID doesn't exist. You can find your ID by viewing your Steam profile and copying the unique name or number shown in the URL bar. Pasting the full URL also works.",
                        helpImageUrl: $"{_configuration.GetWebsiteUrl()}/images/discord/steam_find_your_profile_id.png"
                    );
                }
                else
                {
                    await message.DeleteAsync();
                    return CommandResult.Fail(ex.Error.Message);
                }
            }

            // Load the guild
            var guild = await _db.DiscordGuilds
                .AsNoTracking()
                .Include(x => x.Configurations)
                .FirstOrDefaultAsync(x => x.DiscordId == this.Context.Guild.Id.ToString());
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
                await message.DeleteAsync();
                return CommandResult.Fail(
                    $"Beep boop! I don't support that currency."
                );
            }

            // Reload the profiles inventory
            await message.LoadingAsync("🔄 Fetching inventory details from Steam...");
            _ = await _commandProcessor.ProcessWithResultAsync(new FetchSteamProfileInventoryRequest()
            {
                ProfileId = profile.Id.ToString()
            });

            await _db.SaveChangesAsync();

            // Calculate the profiles inventory totals
            await message.LoadingAsync("💱 Calculating inventory value...");
            var inventoryTotals = await _queryProcessor.ProcessAsync(new GetSteamProfileInventoryTotalsRequest()
            {
                ProfileId = profile.SteamId,
                CurrencyId = currency.SteamId,
            });
            if (inventoryTotals == null)
            {
                await message.DeleteAsync();
                return CommandResult.Fail(
                    reason: $"Private inventory (or no Rust items)",
                    explaination: $"That Steam inventory is either **private** or doesn't contain any **marketable** Rust items. Check your profile privacy and ensure that your inventory is public and that at least one of your items are marketable.",
                    helpImageUrl: $"{_configuration.GetWebsiteUrl()}/images/discord/steam_privacy_public.png"
                );
            }

            // Generate the profiles inventory thumbnail
            await message.LoadingAsync("🎨 Generating inventory thumbnail...");
            var inventoryThumbnail = await _commandProcessor.ProcessWithResultAsync(new GenerateSteamProfileInventoryThumbnailRequest()
            {
                ProfileId = profile.SteamId,
                ExpiresOn = DateTimeOffset.Now.AddDays(7)
            });

            await _db.SaveChangesAsync();

            var color = Color.Blue;
            if (profile.Roles.Contains(Roles.VIP))
            {
                color = Color.Gold;
            }

            var author = new EmbedAuthorBuilder()
                .WithName(profile.Name)
                .WithIconUrl(profile.AvatarUrl)
                .WithUrl($"{_configuration.GetWebsiteUrl()}/steam/inventory/{profile.SteamId}");

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
                .WithImageUrl($"{_configuration.GetWebsiteUrl()}/api/image/{inventoryThumbnail?.Image?.Id}")
                .WithThumbnailUrl(profile.AvatarUrl)
                .WithColor(color)
                .WithFooter(x => x.Text = _configuration.GetWebsiteUrl())
                .Build();

            await message.DeleteAsync();
            return CommandResult.Success(
                embed: embed
            );
        }
    }
}
