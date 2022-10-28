using CommandQuery;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Bot.Server.Autocompleters;
using SCMM.Discord.Client.Commands;
using SCMM.Discord.Client.Extensions;
using SCMM.Discord.Data.Store;
using SCMM.Shared.API.Extensions;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Store;
using System.Text;

namespace SCMM.Discord.Bot.Server.Modules;

public class ItemModule : InteractionModuleBase<ShardedInteractionContext>
{
    private readonly IConfiguration _configuration;
    private readonly DiscordDbContext _discordDb;
    private readonly SteamDbContext _steamDb;
    private readonly ICommandProcessor _commandProcessor;
    private readonly IQueryProcessor _queryProcessor;

    public ItemModule(IConfiguration configuration, DiscordDbContext discordDb, SteamDbContext steamDb, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
    {
        _configuration = configuration;
        _discordDb = discordDb;
        _steamDb = steamDb;
        _commandProcessor = commandProcessor;
        _queryProcessor = queryProcessor;
    }

    [SlashCommand("skin", "Show skin details and prices")]
    public async Task<RuntimeResult> GetItemValueAsync(
        [Summary("name", "Name of the skin")][Autocomplete(typeof(ItemNameAutocompleteHandler))] string name,
        [Summary("currency", "Any supported three-letter currency code (e.g. USD, EUR, AUD)")][Autocomplete(typeof(CurrencyAutocompleteHandler))] string currencyId = null
    )
    {
        // If currency was not specified, default to the user or guild currency (if any)
        if (string.IsNullOrEmpty(currencyId) && Context.User != null)
        {
            var user = await _discordDb.DiscordUsers
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

        // Find the closest item name to the one specified (accounting for minor typos)
        var closestItemName = _steamDb.SteamAssetDescriptions
            .Select(x => x.Name)
            .ToList()
            .Closest(x => x, name, maxDistance: 3);

        if (String.IsNullOrEmpty(closestItemName))
        {
            return InteractionResult.Fail(
                $"Sorry, I can't find that item."
            );
        }

        // Load the item
        var appId = _configuration.GetDiscordConfiguration().AppId;
        var item = await _steamDb.SteamAssetDescriptions
            .Where(x => x.App.SteamId == appId.ToString())
            .Where(x => x.Name == closestItemName)
            .Include(x => x.App)
            .Include(x => x.StoreItem).ThenInclude(x => x.Stores)
            .Include(x => x.StoreItem).ThenInclude(x => x.Currency)
            .Include(x => x.MarketItem).ThenInclude(x => x.Currency)
            .FirstOrDefaultAsync();

        if (item == null)
        {
            return InteractionResult.Fail(
                $"Sorry, I can't find that item."
            );
        }

        var buyPrices = item.GetBuyPrices(currency).ToList();
        var steamStorePrice = buyPrices.FirstOrDefault(x => x.MarketType == MarketType.SteamStore);
        var steamMarketPrice = buyPrices.FirstOrDefault(x => x.MarketType == MarketType.SteamCommunityMarket);
        var competitiveMarketPrices = buyPrices
            .Where(x => steamMarketPrice == null || x.Price <= steamMarketPrice.Price)
            .Where(x => x.IsAvailable)
            .OrderBy(x => x.Price)
            .ToList();

        var description = new StringBuilder(item.Description);
        if (steamStorePrice != null && item.TimeAccepted != null)
        {
            if (!description.ToString().EndsWith("."))
            {
                description.Append(".");
            }

            description.AppendLine();
            description.AppendLine();
            description.Append($"{(item.StoreItem.Stores.Count > 1 ? "First released" : "Released")} on **{item.TimeAccepted.Value.ToString("dd MMMM yyyy")}**");
            if (!steamStorePrice.IsAvailable)
            {
                description.Append($" for **{currency.ToPriceString(steamStorePrice.Price)}**");
            }
            if (item.SupplyTotalEstimated > 0)
            {
                description.Append($" with **{item.SupplyTotalEstimated.Value.ToQuantityString()}+** total estimated supply");
            }
            if (!description.ToString().EndsWith("."))
            {
                description.Append(".");
            }
        }

        var fields = new List<EmbedFieldBuilder>();
        foreach (var price in competitiveMarketPrices.Take(25))
        {
            var priceAvailabilityFormatter = (!price.IsAvailable ? "~~" : null);
            var priceIsCheapest = (price.Price == competitiveMarketPrices.Min(x => x.Price));
            var priceColorFormatter = String.Empty;
            if (priceIsCheapest)
            {
                priceColorFormatter = "yaml";
            }
            else
            {
                priceColorFormatter = "fix";
            }

            var priceName = new StringBuilder(price.MarketType.GetDisplayName());
            if (price.Supply > 0)
            {
                priceName.Append($" ({price.Supply.Value.ToQuantityString()})");
            }

            var priceValue = new StringBuilder();
            priceValue.Append(priceAvailabilityFormatter);
            priceValue.Append($"```{priceColorFormatter}");
            priceValue.AppendLine();
            priceValue.Append(currency.ToPriceString(price.Price));
            priceValue.AppendLine();
            priceValue.Append($"```");
            priceValue.Append(priceAvailabilityFormatter);
            if (price.IsAvailable && priceIsCheapest)
            {
                priceValue.Append($"[buy now at the cheapest price]({price.Url})");
            }

            fields.Add(
                new EmbedFieldBuilder()
                    .WithName(priceName.ToString())
                    .WithValue(priceValue.ToString())
                    .WithIsInline(false)
            );
        }

        var embed = new EmbedBuilder()
            .WithTitle(item.Name)
            .WithUrl($"{_configuration.GetWebsiteUrl()}/item/{Uri.EscapeDataString(item.Name)}")
            .WithDescription(description.ToString())
            .WithThumbnailUrl(item.IconLargeUrl ?? item.IconUrl)
            .WithImageUrl(item.PreviewUrl ?? item.IconLargeUrl ?? item.IconUrl)
            .WithColor(Convert.ToUInt32(item.ForegroundColour.Trim('#'), 16))
            .WithFooter(x => x.Text = _configuration.GetWebsiteUrl());

        if (fields.Any())
        {
            embed = embed.WithFields(fields);
        }

        return InteractionResult.Success(
            embed: embed.Build()
        );
    }
}
