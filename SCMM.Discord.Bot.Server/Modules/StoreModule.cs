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
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Store;
using System.Globalization;
using System.Text;

namespace SCMM.Discord.Bot.Server.Modules;

[Group("store", "Store commands")]
public class StoreModule : InteractionModuleBase<ShardedInteractionContext>
{
    private readonly IConfiguration _configuration;
    private readonly DiscordDbContext _discordDb;
    private readonly SteamDbContext _steamDb;
    private readonly ICommandProcessor _commandProcessor;
    private readonly IQueryProcessor _queryProcessor;

    public StoreModule(IConfiguration configuration, DiscordDbContext discordDb, SteamDbContext steamDb, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
    {
        _configuration = configuration;
        _discordDb = discordDb;
        _steamDb = steamDb;
        _commandProcessor = commandProcessor;
        _queryProcessor = queryProcessor;
    }

    [SlashCommand("items", "Show store details")]
    public async Task<RuntimeResult> GetItemValueAsync(
        [Summary("from", "The name of the store you want to view items from")][Autocomplete(typeof(StoreNameAutocompleteHandler))] string storeId,
        [Summary("currency", "Any supported three-letter currency code (e.g. USD, EUR, AUD)")][Autocomplete(typeof(CurrencyAutocompleteHandler))] string currencyId = null
    )
    {
        // If currency was not specified, default to the profile or guild currency (if any)
        if (string.IsNullOrEmpty(currencyId) && Context.User != null)
        {
            var discordId = Context.User.GetFullUsername();
            currencyId = await _steamDb.SteamProfiles
                .Where(x => x.DiscordId == discordId)
                .Where(x => x.Currency != null)
                .Select(x => x.Currency.Name)
                .FirstOrDefaultAsync();
        }
        if (string.IsNullOrEmpty(currencyId) && Context.Guild != null)
        {
            var guildId = Context.Guild.Id;
            var guild = await _discordDb.DiscordGuilds
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == guildId);
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

        // Find the store
        var appId = _configuration.GetDiscordConfiguration().AppId;
        var store = _steamDb.SteamItemStores
            .Include(x => x.App)
            .Include(x => x.Items).ThenInclude(x => x.Item).ThenInclude(x => x.Currency)
            .Include(x => x.Items).ThenInclude(x => x.Item).ThenInclude(x => x.Description)
            .Where(x => x.App.SteamId == appId.ToString())
            .FirstOrDefault(x => x.Id.ToString() == storeId);

        if (store == null)
        {
            return InteractionResult.Fail(
                $"Sorry, I can't find that store."
            );
        }

        var description = new StringBuilder();
        if (store.Start != null)
        {
            if (store.End == null)
            {
                var nextUpdateTime = await _queryProcessor.ProcessAsync(new GetStoreNextUpdateTimeRequest()
                {
                    AppId = _configuration.GetDiscordConfiguration().AppId
                });
                if (nextUpdateTime != null)
                {
                    description.Append($"This store is currently live with approximately {nextUpdateTime.TimeRemaining.ToDurationString(showSeconds: false, maxGranularity: 2)} remaining until it ends.");
                }
                else
                {
                    description.Append($"This store is currently live with no foreseeable end date.");
                }
            }
            else
            {
                description.Append($"This store was available between {store.Start.Value.ToString("MMMM d")} and {store.End.Value.ToString("MMMM d")}.");
            }
        }
        else
        {
            if (store.End == null)
            {
                description.Append($"This store is currently live with no foreseeable end date.");
            }
            else
            {
                description.Append($"This store ended on {store.End.Value.ToString("MMMM d")}.");
            }
        }

        var fields = new List<EmbedFieldBuilder>();
        var sortedItems = store.Items.OrderByDescending(x => (x.Item.Description.SupplyTotalEstimated ?? 0)).ThenByDescending(x => (x.Item.Description.SubscriptionsLifetime ?? 0));
        foreach (var item in sortedItems)
        {
            var itemValue = new StringBuilder();
            itemValue.Append($"`{currency.ToPriceString(currency.CalculateExchange(item.Item.Price ?? 0, item.Item.Currency))}`");
            if (item.Item.Description.SupplyTotalEstimated > 0)
            {
                itemValue.Append($" with {item.Item.Description.SupplyTotalEstimated.Value.ToQuantityString()}+ estimated sales");
            }
            itemValue.Append($" ([view]({_configuration.GetWebsiteUrl()}/item/{Uri.EscapeDataString(item.Item.Description.Name)}))");

            fields.Add(
                new EmbedFieldBuilder()
                    .WithName(item.Item.Description.Name)
                    .WithValue(itemValue)
                    .WithIsInline(false)
            );
        }

        var embed = new EmbedBuilder()
            .WithTitle($"{(store.Start != null ? store.Start.Value.ToString("yyyy MMMM d") : null)}{(store.Start != null ? store.Start.Value.GetDaySuffix() : null)} - {store.Name}".Trim(' ', '-'))
            .WithDescription(description.ToString())
            .WithUrl($"{_configuration.GetWebsiteUrl()}/store/{store.StoreId()}")
            .WithThumbnailUrl(store.App.IconUrl)
            .WithImageUrl(store.ItemsThumbnailUrl)
            .WithColor(UInt32.Parse(store.App.PrimaryColor.Trim('#'), NumberStyles.HexNumber))
            .WithFooter(x => x.Text = _configuration.GetWebsiteUrl());

        if (fields.Any())
        {
            embed = embed.WithFields(fields);
        }

        return InteractionResult.Success(
            embed: embed.Build()
        );
    }

    [SlashCommand("next", "Show time remaining until the next store update")]
    public async Task<RuntimeResult> GetStoreNextUpdateExpectedOnAsync()
    {
        var nextUpdateTime = await _queryProcessor.ProcessAsync(new GetStoreNextUpdateTimeRequest()
        {
            AppId = _configuration.GetDiscordConfiguration().AppId
        });
        if (nextUpdateTime == null || string.IsNullOrEmpty(nextUpdateTime.TimeDescription))
        {
            return InteractionResult.Fail(
                $"The configured application either does not support stores, or has had no previous stores for me to compare against."
            );
        }

        return InteractionResult.Success(
            $"Next store update is {nextUpdateTime.TimeDescription}."
        );
    }
}
