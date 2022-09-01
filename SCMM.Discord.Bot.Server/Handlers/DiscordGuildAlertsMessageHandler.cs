using Discord;
using Microsoft.EntityFrameworkCore;
using SCMM.Azure.ServiceBus;
using SCMM.Discord.Client;
using SCMM.Discord.Data.Store;
using SCMM.Shared.API.Extensions;
using SCMM.Shared.API.Messages;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using System.Globalization;
using System.Text;
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
        IMessageHandler<MarketItemPriceProfitableDealDetectedMessage>,
        IMessageHandler<StoreItemAddedMessage>,
        IMessageHandler<StoreMediaAddedMessage>,
        IMessageHandler<WorkshopFilePublishedMessage>,
        IMessageHandler<WorkshopFileUpdatedMessage>
    {
        private readonly IConfiguration _configuration;
        private readonly DiscordDbContext _discordDb;
        private readonly DiscordClient _client;

        public DiscordGuildAlertsMessageHandler(IConfiguration configuration, DiscordDbContext discordDb, DiscordClient client)
        {
            _configuration = configuration;
            _discordDb = discordDb;
            _client = client;
        }

        public async Task HandleAsync(AppItemDefinitionsUpdatedMessage appItemDefinition, MessageContext context)
        {
            await SendAlertToGuilds(DiscordGuild.GuildConfiguration.AlertChannelAppItemDefinitionsUpdated, (guild, channel) =>
            {
                var description = new StringBuilder();
                description.Append($"In-game item definitions for {appItemDefinition.AppName} have been updated.");

                var fields = new Dictionary<string, string>()
                {
                    { "Digest", $"````{appItemDefinition.ItemDefinitionsDigest}````" }
                };

                return _client.SendMessageAsync(
                    guild.Id,
                    new[] { channel },
                    title: $"{appItemDefinition.AppName} Item Definitions Updated",
                    url: $"{_configuration.GetWebsiteUrl()}/items",
                    thumbnailUrl: !String.IsNullOrEmpty(appItemDefinition.AppIconUrl) ? appItemDefinition.AppIconUrl : null,
                    description: description.ToString(),
                    fields: fields,
                    color: !String.IsNullOrEmpty(appItemDefinition.AppColour) ? (uint?)UInt32.Parse(appItemDefinition.AppColour.Replace("#", ""), NumberStyles.HexNumber) : null,
                    crossPost: true
                );
            });
        }

        public async Task HandleAsync(ItemDefinitionAddedMessage itemDefinition, MessageContext context)
        {
            await SendAlertToGuilds(DiscordGuild.GuildConfiguration.AlertChannelAppItemDefinitionsUpdated, (guild, channel) =>
            {
                var description = new StringBuilder();
                description.Append($"New {itemDefinition.ItemType} has been added to the {itemDefinition.AppName} in-game item definitions.");

                return _client.SendMessageAsync(
                    guild.Id,
                    new[] { channel },
                    authorIconUrl: !String.IsNullOrEmpty(itemDefinition.CreatorAvatarUrl) ? itemDefinition.CreatorAvatarUrl : null,
                    authorName: !String.IsNullOrEmpty(itemDefinition.CreatorName) ? itemDefinition.CreatorName : null,
                    authorUrl: itemDefinition.CreatorId == null ? null : new SteamProfileMyWorkshopFilesPageRequest()
                    {
                        SteamId = itemDefinition.CreatorId.ToString(),
                        AppId = itemDefinition.AppId.ToString()
                    },
                    title: itemDefinition.ItemName,
                    url: $"{_configuration.GetWebsiteUrl()}/item/{itemDefinition.ItemName}",
                    thumbnailUrl: !String.IsNullOrEmpty(itemDefinition.ItemShortName) ? $"{_configuration.GetWebsiteUrl()}/images/app/{itemDefinition.AppId}/items/{itemDefinition.ItemShortName}.png" : null,
                    description: description.ToString(),
                    imageUrl: !String.IsNullOrEmpty(itemDefinition.ItemImageUrl) ? itemDefinition.ItemImageUrl : null,
                    color: !String.IsNullOrEmpty(itemDefinition.AppColour) ? (uint?)UInt32.Parse(itemDefinition.AppColour.Replace("#", ""), NumberStyles.HexNumber) : null,
                    crossPost: true
                );
            });
        }

        public async Task HandleAsync(MarketItemAddedMessage marketItem, MessageContext context)
        {
            await SendAlertToGuilds(DiscordGuild.GuildConfiguration.AlertChannelMarketItemAdded, (guild, channel) =>
            {
                var description = new StringBuilder();
                description.Append($"New {marketItem.ItemType} has been listed on the {marketItem.AppName} community market.");
                
                return _client.SendMessageAsync(
                    guild.Id,
                    new[] { channel },
                    authorIconUrl: !String.IsNullOrEmpty(marketItem.CreatorAvatarUrl) ? marketItem.CreatorAvatarUrl : null,
                    authorName: marketItem.CreatorName,
                    authorUrl: new SteamProfileMyWorkshopFilesPageRequest()
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
                    thumbnailUrl: !String.IsNullOrEmpty(marketItem.ItemShortName) ? $"{_configuration.GetWebsiteUrl()}/images/app/{marketItem.AppId}/items/{marketItem.ItemShortName}.png" : null,
                    description: description.ToString(),
                    imageUrl: !String.IsNullOrEmpty(marketItem.ItemImageUrl) ? marketItem.ItemImageUrl : null,
                    color: !String.IsNullOrEmpty(marketItem.AppColour) ? (uint?)UInt32.Parse(marketItem.AppColour.Replace("#", ""), NumberStyles.HexNumber) : null,
                    crossPost: true
                );
            });
        }

        public async Task HandleAsync(MarketItemManipulationDetectedMessage marketItem, MessageContext context)
        {
            throw new NotImplementedException();
        }

        public async Task HandleAsync(MarketItemPriceAllTimeHighReachedMessage marketItem, MessageContext context)
        {
            throw new NotImplementedException();
        }

        public async Task HandleAsync(MarketItemPriceAllTimeLowReachedMessage marketItem, MessageContext context)
        {
            throw new NotImplementedException();
        }

        public async Task HandleAsync(MarketItemPriceProfitableDealDetectedMessage marketItem, MessageContext context)
        {
            throw new NotImplementedException();
        }

        public async Task HandleAsync(StoreItemAddedMessage storeItem, MessageContext context)
        {
            await SendAlertToGuilds(DiscordGuild.GuildConfiguration.AlertChannelStoreItemAdded, (guild, channel) =>
            {
                var description = new StringBuilder();
                description.Append($"New {storeItem.ItemType} can be purchased from the {storeItem.AppName} store.");

                var fields = new Dictionary<string, string>()
                {
                    { "Prices", String.Join(" ", storeItem.ItemPrices.Select(x => $"`{x.Description} {x.Currency}`")) }
                };

                return _client.SendMessageAsync(
                    guild.Id,
                    new[] { channel },
                    authorIconUrl: !String.IsNullOrEmpty(storeItem.CreatorAvatarUrl) ? storeItem.CreatorAvatarUrl : null,
                    authorName: storeItem.CreatorName,
                    authorUrl: new SteamProfileMyWorkshopFilesPageRequest()
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
                    thumbnailUrl: !String.IsNullOrEmpty(storeItem.ItemShortName) ? $"{_configuration.GetWebsiteUrl()}/images/app/{storeItem.AppId}/items/{storeItem.ItemShortName}.png" : null,
                    description: description.ToString(),
                    fields: fields,
                    imageUrl: !String.IsNullOrEmpty(storeItem.ItemImageUrl) ? storeItem.ItemImageUrl : null,
                    color: !String.IsNullOrEmpty(storeItem.AppColour) ? (uint?)UInt32.Parse(storeItem.AppColour.Replace("#", ""), NumberStyles.HexNumber) : null,
                    crossPost: true
                );
            });
        }

        public async Task HandleAsync(StoreMediaAddedMessage storeMedia, MessageContext context)
        {
            await SendAlertToGuilds(DiscordGuild.GuildConfiguration.AlertChannelStoreMediaAdded, (guild, channel) =>
            {
                var description = new StringBuilder();
                description.Append($"New video discussing the **{storeMedia.StoreName}** {storeMedia.AppName} store.");
                
                // TODO: Check if this is actually a YouTube video first
                return _client.SendMessageAsync(
                    guild.Id,
                    new[] { channel },
                    authorName: storeMedia.ChannelName,
                    authorUrl: $"https://www.youtube.com/channel/{storeMedia.ChannelId}",
                    title: storeMedia.VideoName,
                    url: $"https://www.youtube.com/watch?v={storeMedia.VideoId}",
                    description: description.ToString(),
                    imageUrl: storeMedia.VideoThumbnailUrl,
                    color: new Color(255, 0, 0), // YouTube red
                    crossPost: true
                );
            });
        }

        public async Task HandleAsync(WorkshopFilePublishedMessage workshopFile, MessageContext context)
        {
            await SendAlertToGuilds(DiscordGuild.GuildConfiguration.AlertChannelWorkshopFilePublished, (guild, channel) =>
            {
                var description = new StringBuilder();
                description.Append($"New {workshopFile.ItemType} has been submitted to the {workshopFile.AppName} workshop.");

                var fields = new Dictionary<string, string>();
                if (!String.IsNullOrEmpty(workshopFile.ItemCollection))
                {
                    fields["Collection"] = workshopFile.ItemCollection;
                }

                return _client.SendMessageAsync(
                    guild.Id,
                    new[] { channel },
                    authorIconUrl: !String.IsNullOrEmpty(workshopFile.CreatorAvatarUrl) ? workshopFile.CreatorAvatarUrl : null,
                    authorName: workshopFile.CreatorName,
                    authorUrl: new SteamProfileMyWorkshopFilesPageRequest()
                    {
                        SteamId = workshopFile.CreatorId.ToString(),
                        AppId = workshopFile.AppId.ToString()
                    },
                    title: workshopFile.ItemName,
                    url: new SteamWorkshopFileDetailsPageRequest()
                    {
                        Id = workshopFile.ItemId.ToString()
                    },
                    thumbnailUrl: !String.IsNullOrEmpty(workshopFile.ItemShortName) ? $"{_configuration.GetWebsiteUrl()}/images/app/{workshopFile.AppId}/items/{workshopFile.ItemShortName}.png" : null,
                    description: description.ToString(),
                    fields: fields,
                    imageUrl: !String.IsNullOrEmpty(workshopFile.ItemImageUrl) ? workshopFile.ItemImageUrl : null,
                    color: !String.IsNullOrEmpty(workshopFile.AppColour) ? (uint?)UInt32.Parse(workshopFile.AppColour.Replace("#", ""), NumberStyles.HexNumber) : null,
                    crossPost: true
                );
            });
        }

        public async Task HandleAsync(WorkshopFileUpdatedMessage workshopFile, MessageContext context)
        {
            await SendAlertToGuilds(DiscordGuild.GuildConfiguration.AlertChannelWorkshopFileUpdated, (guild, channel) =>
            {
                var description = new StringBuilder();
                description.Append($"This accepted item has been updated in the {workshopFile.AppName} workshop. Depending on the change made, there may be in-game visual changes to the apparence of the item.");

                var fields = new Dictionary<string, string>()
                {
                    { "Changes", String.IsNullOrEmpty(workshopFile.ChangeNote) ? "Unknown" : workshopFile.ChangeNote }
                };

                return _client.SendMessageAsync(
                    guild.Id,
                    new[] { channel },
                    authorIconUrl: !String.IsNullOrEmpty(workshopFile.CreatorAvatarUrl) ? workshopFile.CreatorAvatarUrl : null,
                    authorName: workshopFile.CreatorName,
                    authorUrl: new SteamProfileMyWorkshopFilesPageRequest()
                    {
                        SteamId = workshopFile.CreatorId.ToString(),
                        AppId = workshopFile.AppId.ToString()
                    },
                    title: workshopFile.ItemName,
                    url: new SteamWorkshopFileDetailsPageRequest()
                    {
                        Id = workshopFile.ItemId.ToString()
                    },
                    thumbnailUrl: !String.IsNullOrEmpty(workshopFile.ItemShortName) ? $"{_configuration.GetWebsiteUrl()}/images/app/{workshopFile.AppId}/items/{workshopFile.ItemShortName}.png" : null,
                    description: description.ToString(),
                    fields: fields,
                    imageUrl: !String.IsNullOrEmpty(workshopFile.ItemImageUrl) ? workshopFile.ItemImageUrl : null,
                    color: !String.IsNullOrEmpty(workshopFile.AppColour) ? (uint?) UInt32.Parse(workshopFile.AppColour.Replace("#", ""), NumberStyles.HexNumber) : null,
                    crossPost: true
                );
            });
        }

        private async Task SendAlertToGuilds(string alertConfigurationName, Func<DiscordGuild, string, Task> alertCallback)
        {
            var guildsWithAlertsEnabled = await _discordDb.DiscordGuilds.AsNoTracking()
                .Where(x => (x.Flags & DiscordGuild.GuildFlags.Alerts) != 0)
                .ToListAsync();

            var guildsToBeAlerted = guildsWithAlertsEnabled
                .Where(x => x.Configuration.Any(x => x.Name == alertConfigurationName && !String.IsNullOrEmpty(x.Value)))
                .ToList();

            var alertTasks = new List<Task>();
            foreach (var guild in guildsToBeAlerted)
            {
                alertTasks.Add(
                    alertCallback(guild, guild.Configuration.FirstOrDefault(x => x.Name == alertConfigurationName).Value)
                );
            }

            await Task.WhenAll(alertTasks);
        }
    }
}
