using CommandQuery;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Client;
using SCMM.Discord.Client.Extensions;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.API.Extensions;
using SCMM.Steam.API;
using SCMM.Steam.API.Commands;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Data.Store;
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

            var message = await Context.Message.ReplyAsync("Loading...");
            await message.LoadingAsync("🔍 Finding Steam profile...");

            // Load the profile using the discord id
            var discordId = user.GetFullUsername();
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
            [Name("steam_id")][Summary("Valid SteamID or Steam URL")] string steamId,
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
                return await SayProfileInventoryValueInternalAsync(null, steamId, currencyId);
            }
        }

        private async Task<RuntimeResult> SayProfileInventoryValueInternalAsync(IUserMessage message, string steamId, string currencyId)
        {
            message = (message ?? await Context.Message.ReplyAsync("Loading..."));

            // Load the profile
            await message.LoadingAsync("🔍 Finding Steam profile...");
            var importedProfile = await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileRequest()
            {
                ProfileId = steamId
            });

            var profile = importedProfile?.Profile;
            if (profile == null)
            {
                await message.DeleteAsync();
                return CommandResult.Fail(
                    reason: $"Steam profile not found",
                    explaination: $"That Steam profile doesn't exist. Supported options are **Steam ID64**, **Custom URL**, or **Profile URL**. You can easily find your Profile URL by viewing your profile in Steam and copying it from the URL bar.",
                    helpImageUrl: $"{_configuration.GetWebsiteUrl()}/images/discord/steam_find_your_profile_id.png"
                );
            }

            // Load the guild
            if (Context.Guild != null)
            {
                var guild = await _db.DiscordGuilds
                    .AsNoTracking()
                    .Include(x => x.Configurations)
                    .FirstOrDefaultAsync(x => x.DiscordId == Context.Guild.Id.ToString());
                if (guild != null && string.IsNullOrEmpty(currencyId))
                {
                    currencyId = guild.Get(DiscordConfiguration.Currency).Value;
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
                await message.DeleteAsync();
                return CommandResult.Fail(
                    $"Sorry, I don't support that currency."
                );
            }

            // Reload the profiles inventory
            await message.LoadingAsync("🔄 Fetching inventory details from Steam...");
            var importedInventory = await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileInventoryRequest()
            {
                ProfileId = profile.Id.ToString()
            });
            if (importedInventory?.Profile?.Privacy != Steam.Data.Models.Enums.SteamVisibilityType.Public)
            {
                await message.DeleteAsync();
                return CommandResult.Fail(
                    reason: $"Private inventory",
                    explaination: $"That Steam inventory is **private**. Check your profile privacy to ensure that your inventory is public, then try again.",
                    helpImageUrl: $"{_configuration.GetWebsiteUrl()}/images/discord/steam_privacy_public.png"
                );
            }

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
                    reason: $"No Rust items found",
                    explaination: $"That Steam inventory doesn't contain any **marketable** Rust items. If you've recently purchased some items, it can take up to 7 days for those items to become marketable."
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
                .WithUrl($"{_configuration.GetWebsiteUrl()}/inventory/{profile.SteamId}");

            var marketIcon = (inventoryTotals.MarketMovementValue > 0) ? "📈" : "📉";
            var marketDirection = (inventoryTotals.MarketMovementValue > 0) ? "Up" : "Down";
            var marketMovement = $"{marketDirection} {currency.ToPriceString(inventoryTotals.MarketMovementValue, dense: true)} {(DateTimeOffset.Now - inventoryTotals.MarketMovementTime).ToDurationString(prefix: "in the last", maxGranularity: 1)}";
            var fields = new List<EmbedFieldBuilder>
            {
                new EmbedFieldBuilder()
                .WithName($"{marketIcon} {currency.ToPriceString(inventoryTotals.MarketValue)}")
                .WithValue(marketMovement)
                .WithIsInline(false)
            };

            var embed = new EmbedBuilder()
                .WithAuthor(author)
                .WithDescription($"Inventory contains **{inventoryTotals.Items.ToQuantityString()}** item(s).")
                .WithFields(fields)
                .WithThumbnailUrl(profile.AvatarUrl)
                .WithColor(color)
                .WithFooter(x => x.Text = _configuration.GetWebsiteUrl());

            if (inventoryThumbnail?.Image?.Id != null)
            {
                embed = embed.WithImageUrl($"{_configuration.GetWebsiteUrl()}/api/image/{inventoryThumbnail.Image.Id}");
            }

            await message.DeleteAsync();
            return CommandResult.Success(
                embed: embed.Build()
            );
        }
    }
}
