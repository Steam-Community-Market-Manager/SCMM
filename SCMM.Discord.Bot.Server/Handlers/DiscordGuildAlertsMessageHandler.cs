using Discord;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Client;
using SCMM.Discord.Client.Extensions;
using SCMM.Discord.Data.Store;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.API.Events;
using SCMM.Shared.API.Extensions;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Models.Store.Requests.Html;
using SCMM.Steam.Data.Store;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DiscordGuild = SCMM.Discord.Data.Store.DiscordGuild;

namespace SCMM.Discord.Bot.Server.Handlers
{
    public class DiscordGuildAlertsMessageHandler :
        IMessageHandler<AppItemDefinitionsUpdatedMessage>,
        IMessageHandler<ItemDefinitionAddedMessage>,
        IMessageHandler<MarketItemAddedMessage>,
        IMessageHandler<MarketItemManipulationDetectedMessage>,
        IMessageHandler<MarketItemPriceAllTimeHighReachedMessage>,
        IMessageHandler<MarketItemPriceAllTimeLowReachedMessage>,
        IMessageHandler<MarketItemPriceProfitableBuyDealDetectedMessage>,
        IMessageHandler<StoreAddedMessage>,
        IMessageHandler<StoreItemAddedMessage>,
        IMessageHandler<StoreMediaAddedMessage>,
        IMessageHandler<WorkshopFilePublishedMessage>,
        IMessageHandler<WorkshopFileUpdatedMessage>
    {
        private readonly ILogger<DiscordGuildAlertsMessageHandler> _logger;
        private readonly IConfiguration _configuration;
        private readonly SteamDbContext _steamDb;
        private readonly DiscordDbContext _discordDb;
        private readonly DiscordConfiguration _discordConfiguration;
        private readonly DiscordClient _client;

        public DiscordGuildAlertsMessageHandler(ILogger<DiscordGuildAlertsMessageHandler> logger, IConfiguration configuration, SteamDbContext steamDb, DiscordDbContext discordDb, DiscordClient client)
        {
            _logger = logger;
            _configuration = configuration;
            _steamDb = steamDb;
            _discordDb = discordDb;
            _discordConfiguration = _configuration.GetDiscordConfiguration();
            _client = client;
        }

        public async Task HandleAsync(AppItemDefinitionsUpdatedMessage appItemDefinition, IMessageContext context)
        {
            //if (appItemDefinition?.AppId != _discordConfiguration.AppId)
            //    return;

            await SendAlertToGuilds(DiscordGuild.GuildConfiguration.AlertChannelAppItemDefinitionsUpdated, (guildId, channelId) =>
            {
                var description = new StringBuilder();
                description.AppendLine($"In-game item definitions for {appItemDefinition.AppName} have been updated.");
                description.AppendLine($"```{appItemDefinition.ItemDefinitionsDigest}```");

                return _client.SendMessageAsync(
                    guildId,
                    channelId,
                    title: $"{appItemDefinition.AppName} Item Definitions Updated",
                    url: $"{_configuration.GetWebsiteUrl()}/items",
                    thumbnailUrl: !String.IsNullOrEmpty(appItemDefinition.AppIconUrl) ? appItemDefinition.AppIconUrl : null,
                    description: description.ToString(),
                    color: !String.IsNullOrEmpty(appItemDefinition.AppColour) ? (uint?)UInt32.Parse(appItemDefinition.AppColour.Replace("#", ""), NumberStyles.HexNumber) : null,
                    crossPost: AppDomain.CurrentDomain.IsReleaseBuild()
                );
            });
        }

        public async Task HandleAsync(ItemDefinitionAddedMessage itemDefinition, IMessageContext context)
        {
            //if (itemDefinition?.AppId != _discordConfiguration.AppId)
            //    return;

            await SendAlertToGuilds(DiscordGuild.GuildConfiguration.AlertChannelAppItemDefinitionsItemAdded, (guildId, channelId) =>
            {
                var description = new StringBuilder();
                description.Append($"New {itemDefinition.ItemType} has been added to the {itemDefinition.AppName} in-game item definitions.");

                return _client.SendMessageAsync(
                    guildId,
                    channelId,
                    authorIconUrl: !String.IsNullOrEmpty(itemDefinition.CreatorAvatarUrl) ? itemDefinition.CreatorAvatarUrl : null,
                    authorName: !String.IsNullOrEmpty(itemDefinition.CreatorName) ? itemDefinition.CreatorName : null,
                    authorUrl: itemDefinition.CreatorId == null ? null : new SteamProfileMyWorkshopFilesPageRequest()
                    {
                        SteamId = itemDefinition.CreatorId.ToString(),
                        AppId = itemDefinition.AppId.ToString()
                    },
                    title: itemDefinition.ItemName,
                    url: $"{_configuration.GetWebsiteUrl()}/item/{itemDefinition.ItemName}",
                    thumbnailUrl: !String.IsNullOrEmpty(itemDefinition.ItemShortName) ? $"{_configuration.GetDataStoreUrl()}/images/app/{itemDefinition.AppId}/items/{itemDefinition.ItemShortName}.png" : null,
                    description: description.ToString(),
                    imageUrl: !String.IsNullOrEmpty(itemDefinition.ItemImageUrl) ? itemDefinition.ItemImageUrl : null,
                    color: !String.IsNullOrEmpty(itemDefinition.AppColour) ? (uint?)UInt32.Parse(itemDefinition.AppColour.Replace("#", ""), NumberStyles.HexNumber) : null,
                    crossPost: AppDomain.CurrentDomain.IsReleaseBuild()
                );
            });
        }

        public async Task HandleAsync(MarketItemAddedMessage marketItem, IMessageContext context)
        {
            if (marketItem?.AppId != _discordConfiguration.AppId)
                return;

            await SendAlertToGuilds(DiscordGuild.GuildConfiguration.AlertChannelMarketItemAdded, (guildId, channelId) =>
            {
                var description = new StringBuilder();
                description.Append($"New {marketItem.ItemType} has been listed on the {marketItem.AppName} community market.");

                return _client.SendMessageAsync(
                    guildId,
                    channelId,
                    authorIconUrl: !String.IsNullOrEmpty(marketItem.CreatorAvatarUrl) ? marketItem.CreatorAvatarUrl : null,
                    authorName: marketItem.CreatorName,
                    authorUrl: marketItem.CreatorId == null ? null : new SteamProfileMyWorkshopFilesPageRequest()
                    {
                        SteamId = marketItem.CreatorId.ToString(),
                        AppId = marketItem.AppId.ToString()
                    },
                    title: marketItem.ItemName,
                    url: new SteamMarketListingPageRequest()
                    {
                        AppId = marketItem.AppId.ToString(),
                        MarketHashName = marketItem.ItemName
                    },
                    thumbnailUrl: !String.IsNullOrEmpty(marketItem.ItemIconUrl) ? marketItem.ItemIconUrl :
                                  !String.IsNullOrEmpty(marketItem.ItemShortName) ? $"{_configuration.GetDataStoreUrl()}/images/app/{marketItem.AppId}/items/{marketItem.ItemShortName}.png" : null,
                    description: description.ToString(),
                    imageUrl: !String.IsNullOrEmpty(marketItem.ItemImageUrl) ? marketItem.ItemImageUrl : null,
                    color: !String.IsNullOrEmpty(marketItem.AppColour) ? (uint?)UInt32.Parse(marketItem.AppColour.Replace("#", ""), NumberStyles.HexNumber) : null,
                    crossPost: AppDomain.CurrentDomain.IsReleaseBuild()
                );
            });
        }

        public async Task HandleAsync(MarketItemManipulationDetectedMessage marketItem, IMessageContext context)
        {
            if (marketItem?.AppId != _discordConfiguration.AppId)
                return;

            // Only send alerts for new/active manipulations
            if (!marketItem.IsBeingManipulated)
            {
                return;
            }

            await SendAlertToGuilds(DiscordGuild.GuildConfiguration.AlertChannelMarketItemManipulationDetected, (guildId, channelId) =>
            {
                var description = new StringBuilder();
                var fields = (IDictionary<string, string>)null;
                if (marketItem.IsBeingManipulated)
                {
                    description.Append($"Appears to be undergoing market manipulation.");
                    if (!String.IsNullOrEmpty(marketItem.ManipulationReason))
                    {
                        fields = new Dictionary<string, string>()
                        {
                            { "Reason", marketItem.ManipulationReason }
                        };
                    }
                }
                else
                {
                    description.Append($"Manipulation on this item appears to be settling, market activity has returned to predictable levels.");
                }

                return _client.SendMessageAsync(
                    guildId,
                    channelId,
                    title: marketItem.ItemName,
                    url: new SteamMarketListingPageRequest()
                    {
                        AppId = marketItem.AppId.ToString(),
                        MarketHashName = marketItem.ItemName
                    },
                    thumbnailUrl: !String.IsNullOrEmpty(marketItem.ItemIconUrl) ? marketItem.ItemIconUrl :
                                  !String.IsNullOrEmpty(marketItem.ItemShortName) ? $"{_configuration.GetDataStoreUrl()}/images/app/{marketItem.AppId}/items/{marketItem.ItemShortName}.png" : null,
                    description: description.ToString(),
                    fields: fields,
                    color: (marketItem.IsBeingManipulated ? Color.Red : Color.Green),
                    crossPost: AppDomain.CurrentDomain.IsReleaseBuild()
                );
            });
        }

        public async Task HandleAsync(MarketItemPriceAllTimeHighReachedMessage marketItem, IMessageContext context)
        {
            if (marketItem?.AppId != _discordConfiguration.AppId)
                return;

            await SendAlertToGuilds(DiscordGuild.GuildConfiguration.AlertChannelMarketItemPriceAllTimeHighReached, (guildId, channelId) =>
            {
                var description = new StringBuilder();
                description.Append($"Reached a new **all-time-high** price of **{marketItem.AllTimeHighestValueDescription}** on the {marketItem.AppName} community market.");

                return _client.SendMessageAsync(
                    guildId,
                    channelId,
                    title: $"📈 {marketItem.ItemName}",
                    url: new SteamMarketListingPageRequest()
                    {
                        AppId = marketItem.AppId.ToString(),
                        MarketHashName = marketItem.ItemName
                    },
                    thumbnailUrl: !String.IsNullOrEmpty(marketItem.ItemIconUrl) ? marketItem.ItemIconUrl :
                                  !String.IsNullOrEmpty(marketItem.ItemShortName) ? $"{_configuration.GetDataStoreUrl()}/images/app/{marketItem.AppId}/items/{marketItem.ItemShortName}.png" : null,
                    description: description.ToString(),
                    color: Color.Green,
                    crossPost: AppDomain.CurrentDomain.IsReleaseBuild()
                );
            });
        }

        public async Task HandleAsync(MarketItemPriceAllTimeLowReachedMessage marketItem, IMessageContext context)
        {
            if (marketItem?.AppId != _discordConfiguration.AppId)
                return;

            await SendAlertToGuilds(DiscordGuild.GuildConfiguration.AlertChannelMarketItemPriceAllTimeLowReached, (guildId, channelId) =>
            {
                var description = new StringBuilder();
                description.Append($"Reached a new **all-time-low** price of **{marketItem.AllTimeLowestValueDescription}** on the {marketItem.AppName} community market.");

                return _client.SendMessageAsync(
                    guildId,
                    channelId,
                    title: $"📉 {marketItem.ItemName}",
                    url: new SteamMarketListingPageRequest()
                    {
                        AppId = marketItem.AppId.ToString(),
                        MarketHashName = marketItem.ItemName
                    },
                    thumbnailUrl: !String.IsNullOrEmpty(marketItem.ItemIconUrl) ? marketItem.ItemIconUrl :
                                  !String.IsNullOrEmpty(marketItem.ItemShortName) ? $"{_configuration.GetDataStoreUrl()}/images/app/{marketItem.AppId}/items/{marketItem.ItemShortName}.png" : null,
                    description: description.ToString(),
                    color: Color.Red,
                    crossPost: AppDomain.CurrentDomain.IsReleaseBuild()
                );
            });
        }

        public async Task HandleAsync(MarketItemPriceProfitableBuyDealDetectedMessage marketItem, IMessageContext context)
        {
            var app = await _steamDb.SteamApps
                .FirstOrDefaultAsync(x => x.Id == marketItem.AppId);
            var currency = await _steamDb.SteamCurrencies
                .FirstOrDefaultAsync(x => x.Id == marketItem.CurrencyId);
            var assetDescription = await _steamDb.SteamAssetDescriptions
                .Include(x => x.App)
                .FirstOrDefaultAsync(x => x.Id == marketItem.DescriptionId);

            if (app == null || UInt64.Parse(app.SteamId) != _discordConfiguration.AppId)
                return;

            if (currency == null || assetDescription == null)
            {
                return;
            }

            await SendAlertToGuilds(DiscordGuild.GuildConfiguration.AlertChannelMarketItemPriceProfitableBuyDealDetected, async (guildId, channelId) =>
            {
                var marketName = marketItem.BuyNowFrom.GetDisplayName();
                var marketColor = marketItem.BuyNowFrom.GetColor();
                var marketUrl = marketItem.BuyNowFrom.GetBuyFromOptions()?.FirstOrDefault()?.GenerateBuyUrl(
                    assetDescription.App.SteamId, assetDescription.App.Name, assetDescription.ClassId, assetDescription.Name
                );

                var discountPercentage = Math.Round(100 - marketItem.BuyNowPrice.ToPercentage(marketItem.SellOrderLowestPrice), 0);
                var flipProfit = (long)Math.Round(marketItem.SellOrderLowestPrice - (marketItem.SellOrderLowestPrice * EconomyExtensions.MarketFeeMultiplier) - marketItem.BuyNowPrice - marketItem.BuyNowFee, 0);
                var description = new StringBuilder();
                description.Append(
                    $"This item is selling for **{currency.ToPriceString(marketItem.BuyNowPrice)}** on {marketName}, " +
                    $"**{discountPercentage}% cheaper** compared to the {assetDescription.App.Name} community market ({currency.ToPriceString(marketItem.SellOrderLowestPrice)}). " +
                    $"You could flip this item and make **{currency.ToPriceString(flipProfit)} profit** (after market fees)."
                );

                var linkButtons = new Dictionary<string, string>()
                {
                    { "View Deal", marketUrl },
                    { "Compare with Steam", new SteamMarketListingPageRequest() { AppId = assetDescription.App.SteamId, MarketHashName = assetDescription.NameHash } }
                };

                await _client.SendMessageAsync(
                    guildId,
                    channelId,
                    title: $"Get {discountPercentage}% off {assetDescription.Name}",
                    authorName: marketName,
                    authorIconUrl: $"{_configuration.GetDataStoreUrl()}/images/app/{assetDescription.App.SteamId}/markets/{marketItem.BuyNowFrom.ToString().ToLower()}.png",
                    url: marketUrl,
                    thumbnailUrl: !String.IsNullOrEmpty(assetDescription.IconUrl) ? assetDescription.IconUrl :
                                  !String.IsNullOrEmpty(assetDescription.ItemShortName) ? $"{_configuration.GetDataStoreUrl()}/images/app/{marketItem.AppId}/items/{assetDescription.ItemShortName}.png" : null,
                    description: description.ToString(),
                    color: !String.IsNullOrEmpty(marketColor) ? (uint?)UInt32.Parse(marketColor.Replace("#", ""), NumberStyles.HexNumber) : null,
                    // TODO: Enable this if/when Discord allows us to add components to cross-posted messages
                    //linkButtons: linkButtons, 
                    crossPost: AppDomain.CurrentDomain.IsReleaseBuild()
                );

                try
                {
                    await _client.SendMessageAsync(
                        guildId,
                        channelId,
                        threadName: marketName,
                        title: $"Get {discountPercentage}% off {assetDescription.Name}",
                        authorName: marketName,
                        authorIconUrl: $"{_configuration.GetDataStoreUrl()}/images/app/{assetDescription.App.SteamId}/markets/{marketItem.BuyNowFrom.ToString().ToLower()}.png",
                        url: marketUrl,
                        thumbnailUrl: !String.IsNullOrEmpty(assetDescription.IconUrl) ? assetDescription.IconUrl :
                                      !String.IsNullOrEmpty(assetDescription.ItemShortName) ? $"{_configuration.GetDataStoreUrl()}/images/app/{marketItem.AppId}/items/{assetDescription.ItemShortName}.png" : null,
                        description: description.ToString(),
                        color: !String.IsNullOrEmpty(marketColor) ? (uint?)UInt32.Parse(marketColor.Replace("#", ""), NumberStyles.HexNumber) : null,
                        linkButtons: linkButtons
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to post 'market item price profitable buy deal detected' (id: {marketItem.Id} ) to channel thread");
                }
            });
        }

        public async Task HandleAsync(StoreAddedMessage store, IMessageContext context)
        {
            if (store?.AppId != _discordConfiguration.AppId)
                return;

            await SendAlertToGuilds(DiscordGuild.GuildConfiguration.AlertChannelStoreAdded, (guildId, channelId) =>
            {
                var description = new StringBuilder();
                description.Append($"{store.Items?.Length ?? 0} new item(s) have been added to the {store.AppName} store.");

                var fields = store.Items.Take(25).ToDictionary(
                    x => x.Name,
                    x => x.PriceDescription ?? "N/A"
                );

                return _client.SendMessageAsync(
                    guildId,
                    channelId,
                    title: $"{store.AppName} Store - {store.StoreName}",
                    url: $"{_configuration.GetWebsiteUrl()}/store/{store.StoreId}",
                    thumbnailUrl: !String.IsNullOrEmpty(store.AppIconUrl) ? store.AppIconUrl : null,
                    description: description.ToString(),
                    fields: fields,
                    imageUrl: !String.IsNullOrEmpty(store.ItemsImageUrl) ? store.ItemsImageUrl : null,
                    color: !String.IsNullOrEmpty(store.AppColour) ? (uint?)UInt32.Parse(store.AppColour.Replace("#", ""), NumberStyles.HexNumber) : null,
                    crossPost: AppDomain.CurrentDomain.IsReleaseBuild()
                );
            });
        }

        public async Task HandleAsync(StoreItemAddedMessage storeItem, IMessageContext context)
        {
            if (storeItem?.AppId != _discordConfiguration.AppId)
                return;

            await SendAlertToGuilds(DiscordGuild.GuildConfiguration.AlertChannelStoreItemAdded, (guildId, channelId) =>
            {
                var description = new StringBuilder();
                var defaultPrice = storeItem.ItemPrices?.FirstOrDefault(x => x.Currency == Steam.Data.Models.Constants.SteamDefaultCurrency);
                description.Append($"New {storeItem.ItemType} can be purchased from the {storeItem.AppName} store for **{defaultPrice?.Description}**.");

                return _client.SendMessageAsync(
                    guildId,
                    channelId,
                    authorIconUrl: !String.IsNullOrEmpty(storeItem.CreatorAvatarUrl) ? storeItem.CreatorAvatarUrl : null,
                    authorName: storeItem.CreatorName,
                    authorUrl: storeItem.CreatorId == null ? null : new SteamProfileMyWorkshopFilesPageRequest()
                    {
                        SteamId = storeItem.CreatorId.ToString(),
                        AppId = storeItem.AppId.ToString()
                    },
                    title: storeItem.ItemName,
                    url: new SteamItemStoreDetailPageRequest()
                    {
                        AppId = storeItem.AppId.ToString(),
                        ItemId = storeItem.ItemId.ToString()
                    },
                    thumbnailUrl: !String.IsNullOrEmpty(storeItem.ItemIconUrl) ? storeItem.ItemIconUrl :
                                  !String.IsNullOrEmpty(storeItem.ItemShortName) ? $"{_configuration.GetDataStoreUrl()}/images/app/{storeItem.AppId}/items/{storeItem.ItemShortName}.png" : null,
                    description: description.ToString(),
                    imageUrl: !String.IsNullOrEmpty(storeItem.ItemImageUrl) ? storeItem.ItemImageUrl : null,
                    color: !String.IsNullOrEmpty(storeItem.AppColour) ? (uint?)UInt32.Parse(storeItem.AppColour.Replace("#", ""), NumberStyles.HexNumber) : null,
                    crossPost: AppDomain.CurrentDomain.IsReleaseBuild()
                );
            });
        }

        public async Task HandleAsync(StoreMediaAddedMessage storeMedia, IMessageContext context)
        {
            if (storeMedia?.AppId != _discordConfiguration.AppId)
                return;

            await SendAlertToGuilds(DiscordGuild.GuildConfiguration.AlertChannelStoreMediaAdded, (guildId, channelId) =>
            {
                var description = new StringBuilder();
                description.Append($"New video has been uploaded that discusses the **{storeMedia.StoreName}** {storeMedia.AppName} store.");

                // TODO: Check if this is actually a YouTube video first
                return _client.SendMessageAsync(
                    guildId,
                    channelId,
                    authorName: storeMedia.ChannelName,
                    authorUrl: $"https://www.youtube.com/channel/{storeMedia.ChannelId}",
                    title: storeMedia.VideoName,
                    url: $"https://www.youtube.com/watch?v={storeMedia.VideoId}",
                    description: description.ToString(),
                    imageUrl: storeMedia.VideoThumbnailUrl,
                    color: new Color(255, 0, 0), // YouTube red
                    crossPost: AppDomain.CurrentDomain.IsReleaseBuild()
                );
            });
        }

        public async Task HandleAsync(WorkshopFilePublishedMessage workshopFile, IMessageContext context)
        {
            if (workshopFile?.AppId != _discordConfiguration.AppId)
                return;

            await SendAlertToGuilds(DiscordGuild.GuildConfiguration.AlertChannelWorkshopFilePublished, (guildId, channelId) =>
            {
                var description = new StringBuilder();
                description.Append($"New {workshopFile.ItemType} has been submitted to the {workshopFile.AppName} workshop.");

                var fields = new Dictionary<string, string>();
                if (!String.IsNullOrEmpty(workshopFile.ItemCollection))
                {
                    fields["Collection"] = workshopFile.ItemCollection;
                }

                return _client.SendMessageAsync(
                    guildId,
                    channelId,
                    authorIconUrl: !String.IsNullOrEmpty(workshopFile.CreatorAvatarUrl) ? workshopFile.CreatorAvatarUrl : null,
                    authorName: workshopFile.CreatorName,
                    authorUrl: workshopFile.CreatorId == 0 ? null : new SteamProfileMyWorkshopFilesPageRequest()
                    {
                        SteamId = workshopFile.CreatorId.ToString(),
                        AppId = workshopFile.AppId.ToString()
                    },
                    title: workshopFile.ItemName,
                    url: new SteamWorkshopFileDetailsPageRequest()
                    {
                        Id = workshopFile.ItemId.ToString()
                    },
                    thumbnailUrl: !String.IsNullOrEmpty(workshopFile.ItemIconUrl) ? workshopFile.ItemIconUrl :
                                  !String.IsNullOrEmpty(workshopFile.ItemShortName) ? $"{_configuration.GetDataStoreUrl()}/images/app/{workshopFile.AppId}/items/{workshopFile.ItemShortName}.png" : null,
                    description: description.ToString(),
                    fields: fields,
                    imageUrl: !String.IsNullOrEmpty(workshopFile.ItemImageUrl) ? workshopFile.ItemImageUrl : null,
                    color: !String.IsNullOrEmpty(workshopFile.AppColour) ? (uint?)UInt32.Parse(workshopFile.AppColour.Replace("#", ""), NumberStyles.HexNumber) : null,
                    crossPost: AppDomain.CurrentDomain.IsReleaseBuild()
                );
            });
        }

        public async Task HandleAsync(WorkshopFileUpdatedMessage workshopFile, IMessageContext context)
        {
            if (workshopFile?.AppId != _discordConfiguration.AppId)
                return;

            await SendAlertToGuilds(DiscordGuild.GuildConfiguration.AlertChannelWorkshopFileUpdated, (guildId, channelId) =>
            {
                var workshopFileChangeNotesPageUrl = new SteamWorkshopFileChangeNotesPageRequest()
                {
                    Id = workshopFile.ItemId.ToString()
                };

                var description = new StringBuilder();
                description.AppendLine($"This accepted item has recently been updated in the {workshopFile.AppName} workshop. Depending on the change that was made, there may be in-game visual changes to the apparence of the item.");
                if ((workshopFile.ChangeTimestamp - workshopFile.ItemTimeAccepted) > TimeSpan.Zero)
                {
                    description.AppendLine();
                    description.AppendLine($"It has been {(workshopFile.ChangeTimestamp - workshopFile.ItemTimeAccepted).ToDurationString(maxGranularity: 1)} since the item was accepted in-game.");
                }
                description.AppendLine();
                description.AppendLine($"[view change notes history]({workshopFileChangeNotesPageUrl})");

                var fields = new Dictionary<string, string>();
                if (!String.IsNullOrEmpty(workshopFile.ChangeNote))
                {
                    fields["Most Recent Change"] = workshopFile.ChangeNote;
                }

                return _client.SendMessageAsync(
                    guildId,
                    channelId,
                    authorIconUrl: !String.IsNullOrEmpty(workshopFile.CreatorAvatarUrl) ? workshopFile.CreatorAvatarUrl : null,
                    authorName: workshopFile.CreatorName,
                    authorUrl: workshopFile.CreatorId == 0 ? null : new SteamProfileMyWorkshopFilesPageRequest()
                    {
                        SteamId = workshopFile.CreatorId.ToString(),
                        AppId = workshopFile.AppId.ToString()
                    },
                    title: workshopFile.ItemName,
                    url: new SteamWorkshopFileDetailsPageRequest()
                    {
                        Id = workshopFile.ItemId.ToString()
                    },
                    thumbnailUrl: !String.IsNullOrEmpty(workshopFile.ItemIconUrl) ? workshopFile.ItemIconUrl :
                                  !String.IsNullOrEmpty(workshopFile.ItemShortName) ? $"{_configuration.GetDataStoreUrl()}/images/app/{workshopFile.AppId}/items/{workshopFile.ItemShortName}.png" : null,
                    description: description.ToString(),
                    fields: fields,
                    imageUrl: !String.IsNullOrEmpty(workshopFile.ItemImageUrl) ? workshopFile.ItemImageUrl : null,
                    color: !String.IsNullOrEmpty(workshopFile.AppColour) ? (uint?)UInt32.Parse(workshopFile.AppColour.Replace("#", ""), NumberStyles.HexNumber) : null,
                    crossPost: AppDomain.CurrentDomain.IsReleaseBuild()
                );
            });
        }

        private async Task SendAlertToGuilds(string alertConfigurationName, Func<ulong, ulong, Task> alertCallback)
        {
            var guildsWithAlertsEnabled = await _discordDb.DiscordGuilds.AsNoTracking()
                .Where(x => (x.Flags & DiscordGuild.GuildFlags.Alerts) != 0)
                .ToListAsync();

            var guildsToBeAlerted = guildsWithAlertsEnabled
                .Where(x => x.Configuration.Any(x => x.Name == alertConfigurationName && !String.IsNullOrEmpty(x.Value)))
                .ToList();

            var alertTasks = new List<Task>();
            foreach (var guildToBeAlerted in guildsToBeAlerted)
            {
                var alertTarget = guildToBeAlerted.Configuration.FirstOrDefault(x => x.Name == alertConfigurationName).Value;
                var guild = _client.Guilds.FirstOrDefault(x => x.Id == guildToBeAlerted.Id);
                var channel = guild?.Channels
                    .Where(x => x.Id.ToString() == alertTarget || string.Equals($"<#{x.Id}>", alertTarget, StringComparison.InvariantCultureIgnoreCase) || Regex.IsMatch(x.Name, alertTarget))
                    .FirstOrDefault();

                alertTasks.Add(
                    alertCallback(guild.Id, channel.Id)
                );
            }

            await Task.WhenAll(alertTasks);
        }
    }
}
