using Discord;
using Microsoft.EntityFrameworkCore;
using SCMM.Azure.ServiceBus;
using SCMM.Discord.Client;
using SCMM.Discord.Data.Store;
using SCMM.Shared.API.Messages;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using System.Globalization;
using System.Text;
using DiscordGuild = SCMM.Discord.Data.Store.DiscordGuild;

namespace SCMM.Discord.Bot.Server.Handlers
{
    public class DiscordGuildAlertsMessageHandler :
        IMessageHandler<AppItemDefinitionsUpdatedMessage>,
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
        private readonly DiscordDbContext _discordDb;
        private readonly DiscordClient _client;

        public DiscordGuildAlertsMessageHandler(DiscordDbContext discordDb, DiscordClient client)
        {
            _discordDb = discordDb;
            _client = client;
        }

        public async Task HandleAsync(AppItemDefinitionsUpdatedMessage appItemDefinition, MessageContext context)
        {
            throw new NotImplementedException();
        }

        public async Task HandleAsync(MarketItemAddedMessage marketItem, MessageContext context)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public async Task HandleAsync(StoreMediaAddedMessage storeMedia, MessageContext context)
        {
            await SendAlertToGuilds(DiscordGuild.GuildConfiguration.AlertChannelStoreMediaAdded, (guild, channel) =>
            {
                var description = new StringBuilder();
                description.Append($"{storeMedia.ChannelName} has uploaded a new video covering the **{storeMedia.StoreName}** item store");
                
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
                    crossPost: false
                );
            });
        }

        public async Task HandleAsync(WorkshopFilePublishedMessage workshopFile, MessageContext context)
        {
            await SendAlertToGuilds(DiscordGuild.GuildConfiguration.AlertChannelWorkshopFilePublished, (guild, channel) =>
            {
                var description = new StringBuilder();
                description.Append($"{workshopFile.CreatorName} has published a new **{workshopFile.ItemType}** workshop submission ");
                if (!String.IsNullOrEmpty(workshopFile.ItemCollection))
                {
                    description.Append($" in their **{workshopFile.ItemCollection}** collection");
                }

                return _client.SendMessageAsync(
                    guild.Id,
                    new[] { channel },
                    authorIconUrl: workshopFile.CreatorAvatarUrl,
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
                    thumbnailUrl: $"https://rust.scmm.app/images/app/252490/items/{workshopFile.ItemShortName}.png",
                    description: description.ToString(),
                    imageUrl: workshopFile.ItemImageUrl,
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
                description.Append($"{workshopFile.CreatorName} has updated this accepted workshop item. Depending on the change made, there may be in-game visual changes to the apparence of this item.");
                if (!String.IsNullOrEmpty(workshopFile.ChangeNote))
                {
                    description.AppendLine();
                    description.Append($"````{workshopFile.ChangeNote}````");
                }

                return _client.SendMessageAsync(
                    guild.Id,
                    new[] { channel },
                    authorIconUrl: workshopFile.CreatorAvatarUrl,
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
                    thumbnailUrl: $"https://rust.scmm.app/images/app/252490/items/{workshopFile.ItemShortName}.png",
                    description: description.ToString(),
                    imageUrl: workshopFile.ItemImageUrl,
                    color: !String.IsNullOrEmpty(workshopFile.AppColour) ? (uint?) UInt32.Parse(workshopFile.AppColour.Replace("#", ""), NumberStyles.HexNumber) : null,
                    crossPost: false
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
