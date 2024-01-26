using CommandQuery;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Bot.Server.Autocompleters;
using SCMM.Discord.Client.Commands;
using SCMM.Discord.Client.Extensions;
using SCMM.Discord.Data.Store;
using SCMM.Shared.API.Extensions;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.API.Commands;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Data.Store;

namespace SCMM.Discord.Bot.Server.Modules;

[Group("inventory", "Steam inventory commands")]
public class InventoryModule : InteractionModuleBase<ShardedInteractionContext>
{
    private readonly ILogger<InventoryModule> _logger;
    private readonly IConfiguration _configuration;
    private readonly DiscordDbContext _discordDb;
    private readonly SteamDbContext _steamDb;
    private readonly ICommandProcessor _commandProcessor;
    private readonly IQueryProcessor _queryProcessor;

    public InventoryModule(ILogger<InventoryModule> logger, IConfiguration configuration, DiscordDbContext discordDb, SteamDbContext steamDb, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
    {
        _logger = logger;
        _configuration = configuration;
        _discordDb = discordDb;
        _steamDb = steamDb;
        _commandProcessor = commandProcessor;
        _queryProcessor = queryProcessor;
    }

    [SlashCommand("value", "Calculate the market value of a Steam inventory (it must be public)")]
    public async Task<RuntimeResult> GetUserInventoryValueAsync(
        [Summary("steam-id", "Any SteamID or Steam URL")] string steamId = null,
        [Summary("app", "Any supported Steam app")][Autocomplete(typeof(SteamAppAutocompleteHandler))] ulong appId = 0,
        [Summary("currency", "Any supported three-letter currency code (e.g. USD, EUR, AUD)")][Autocomplete(typeof(CurrencyAutocompleteHandler))] string currencyId = null
    )
    {
        var user = (DiscordUser)null;

        // If steam id was not specified, default to the user (if any)
        if (string.IsNullOrEmpty(steamId) && Context.User != null)
        {
            user = user ?? await _discordDb.DiscordUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == Context.User.Id);
            if (user != null)
            {
                steamId = user.SteamId;
            }
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
            ProfileId = steamId,
            ImportFriendsListAsync = true
        });

        await _steamDb.SaveChangesAsync();

        var profile = importedProfile?.Profile;
        if (profile == null)
        {
            return InteractionResult.Fail(
                reason: $"Steam profile not found",
                explaination: $"That Steam profile doesn't exist. Supported options are **Steam ID64**, **Custom URL**, or **Profile URL**. You can easily find your Profile URL by viewing your profile in Steam and copying it from the URL bar.",
                helpImageUrl: $"{_configuration.GetDataStoreUrl()}/images/discord/steam_find_your_profile_id.png"
            );
        }

        // If app was not specified, default to the server configured app
        if (appId <= 0)
        {
            appId = _configuration.GetDiscordConfiguration().AppId;
        }

        // If currency was not specified, default to the user or guild currency (if any)
        if (string.IsNullOrEmpty(currencyId) && Context.User != null)
        {
            user = user ?? await _discordDb.DiscordUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == Context.User.Id);
            if (user != null)
            {
                currencyId = user.CurrencyId;
            }
        }
        if (string.IsNullOrEmpty(currencyId) && Context.Guild != null)
        {
            var guild = await _discordDb.DiscordGuilds
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == Context.Guild.Id);
            if (guild != null)
            {
                currencyId = guild.Get(Discord.Data.Store.DiscordGuild.GuildConfiguration.Currency).Value;
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
            ProfileId = profile.Id.ToString(),
            AppIds = new[] { appId.ToString() }
        });

        await _steamDb.SaveChangesAsync();

        if (importedInventory?.Profile?.Privacy != Steam.Data.Models.Enums.SteamVisibilityType.Public)
        {
            return InteractionResult.Fail(
                reason: $"Private inventory",
                explaination: $"That Steam inventory is **private**. Check your profile privacy to ensure that your inventory is public, then try again.",
                helpImageUrl: $"{_configuration.GetDataStoreUrl()}/images/discord/steam_privacy_public.png"
            );
        }

        // Calculate the profiles inventory totals
        //await message.LoadingAsync("💱 Calculating inventory value...");
        var inventoryTotals = await _commandProcessor.ProcessWithResultAsync(new CalculateSteamProfileInventoryTotalsRequest()
        {
            ProfileId = profile.SteamId,
            AppId = appId.ToString(),
            CurrencyId = currency.SteamId,
        });

        await _steamDb.SaveChangesAsync();

        if (inventoryTotals?.Items <= 0)
        {
            return InteractionResult.Fail(
                reason: $"No marketable items found",
                explaination: $"That Steam inventory doesn't contain any **marketable** items. If you've recently purchased some items, it can take up to 7 days for those items to become marketable."
            );
        }

        // Generate the profiles inventory thumbnail
        //await message.LoadingAsync("🎨 Generating inventory thumbnail...");
        var inventoryThumbnailImageUrl = (string)null;
        try
        {
            inventoryThumbnailImageUrl = (
                await _commandProcessor.ProcessWithResultAsync(new GenerateSteamProfileInventoryThumbnailRequest()
                {
                    ProfileId = profile.SteamId,
                    AppId = appId.ToString(),
                    ExpiresOn = DateTimeOffset.Now.AddDays(7)
                })
            )?.ImageUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to generate profile inventory thumbnail");
        }

        await _steamDb.SaveChangesAsync();

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

        if (!String.IsNullOrEmpty(inventoryThumbnailImageUrl))
        {
            embed = embed.WithImageUrl(inventoryThumbnailImageUrl);
        }

        return InteractionResult.Success(
            embed: embed.Build()
        );
    }
}
