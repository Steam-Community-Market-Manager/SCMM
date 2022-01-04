using CommandQuery;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Bot.Server.Autocompleters;
using SCMM.Discord.Client.Attributes;
using SCMM.Discord.Client.Commands;
using SCMM.Discord.Client.Extensions;
using SCMM.Shared.API.Extensions;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.API.Commands;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Data.Store;

namespace SCMM.Discord.Bot.Server.Modules;

[Global]
[Group("inventory", "Steam inventory commands")]
public class InventoryModule : InteractionModuleBase<ShardedInteractionContext>
{
    private readonly IConfiguration _configuration;
    private readonly SteamDbContext _db;
    private readonly ICommandProcessor _commandProcessor;
    private readonly IQueryProcessor _queryProcessor;

    public InventoryModule(IConfiguration configuration, SteamDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
    {
        _configuration = configuration;
        _db = db;
        _commandProcessor = commandProcessor;
        _queryProcessor = queryProcessor;
    }

    [SlashCommand("value", "Calculate the market value of a Steam inventory (it must be public)")]
    public async Task<RuntimeResult> GetUserInventoryValueAsync(
        [Summary("steam-id", "Any SteamID or Steam URL")] string steamId = null,
        [Summary("currency", "Any supported three-letter currency code (e.g. USD, EUR, AUD)")][Autocomplete(typeof(CurrencyAutocompleteHandler))] string currencyId = null
    )
    {
        // If steam id was not specified, look up the Discord user (if any)
        if (string.IsNullOrEmpty(steamId) && Context.User != null)
        {
            var discordId = Context.User.GetFullUsername();
            steamId = await _db.SteamProfiles
                .Where(x => x.DiscordId == discordId)
                .Select(x => x.SteamId)
                .FirstOrDefaultAsync();
        }
        if (string.IsNullOrEmpty(steamId))
        {
            return InteractionResult.Fail(
                reason: $"You didn't specify a Steam profile",
                explaination: $"You need to specify a Steam ID in the command options or link your Steam account using `/config steam`."
            );
        }

        // Load the profile
        //await message.LoadingAsync("🔍 Finding Steam profile...");
        var importedProfile = await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileRequest()
        {
            ProfileId = steamId
        });

        var profile = importedProfile?.Profile;
        if (profile == null)
        {
            return InteractionResult.Fail(
                reason: $"Steam profile not found",
                explaination: $"That Steam profile doesn't exist. Supported options are **Steam ID64**, **Custom URL**, or **Profile URL**. You can easily find your Profile URL by viewing your profile in Steam and copying it from the URL bar.",
                helpImageUrl: $"{_configuration.GetWebsiteUrl()}/images/discord/steam_find_your_profile_id.png"
            );
        }

        // If currency was not specified, default to the profile or guild currency (if any)
        if (string.IsNullOrEmpty(currencyId) && profile.CurrencyId != null)
        {
            currencyId = await _db.SteamCurrencies
                .Where(x => x.Id == profile.CurrencyId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();
        }
        if (string.IsNullOrEmpty(currencyId) && Context.Guild != null)
        {
            var guildId = Context.Guild.Id.ToString();
            var guild = await _db.DiscordGuilds
                .AsNoTracking()
                .Include(x => x.Configurations)
                .FirstOrDefaultAsync(x => x.DiscordId == guildId);
            if (guild != null)
            {
                currencyId = guild.Get(Steam.Data.Store.DiscordConfiguration.Currency).Value;
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
            return InteractionResult.Fail(
                $"Sorry, I don't support that currency."
            );
        }

        // Reload the profiles inventory
        //await message.LoadingAsync("🔄 Fetching inventory details from Steam...");
        var importedInventory = await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileInventoryRequest()
        {
            ProfileId = profile.Id.ToString()
        });
        if (importedInventory?.Profile?.Privacy != Steam.Data.Models.Enums.SteamVisibilityType.Public)
        {
            return InteractionResult.Fail(
                reason: $"Private inventory",
                explaination: $"That Steam inventory is **private**. Check your profile privacy to ensure that your inventory is public, then try again.",
                helpImageUrl: $"{_configuration.GetWebsiteUrl()}/images/discord/steam_privacy_public.png"
            );
        }

        await _db.SaveChangesAsync();

        // Calculate the profiles inventory totals
        //await message.LoadingAsync("💱 Calculating inventory value...");
        var inventoryTotals = await _queryProcessor.ProcessAsync(new GetSteamProfileInventoryTotalsRequest()
        {
            ProfileId = profile.SteamId,
            CurrencyId = currency.SteamId,
        });
        if (inventoryTotals == null)
        {
            return InteractionResult.Fail(
                reason: $"No Rust items found",
                explaination: $"That Steam inventory doesn't contain any **marketable** Rust items. If you've recently purchased some items, it can take up to 7 days for those items to become marketable."
            );
        }

        // Generate the profiles inventory thumbnail
        //await message.LoadingAsync("🎨 Generating inventory thumbnail...");
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

        var marketIcon = inventoryTotals.MarketMovementValue > 0 ? "📈" : "📉";
        var marketDirection = inventoryTotals.MarketMovementValue > 0 ? "Up" : "Down";
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
            embed = embed.WithImageUrl($"{_configuration.GetWebsiteUrl()}/api/image/{inventoryThumbnail.Image.Id}.{inventoryThumbnail.Image.MimeType.GetFileExtension()}");
        }

        return InteractionResult.Success(
            embed: embed.Build()
        );
    }
}
