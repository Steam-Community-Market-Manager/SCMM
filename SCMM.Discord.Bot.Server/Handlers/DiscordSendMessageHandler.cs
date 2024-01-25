using SCMM.Discord.API.Messages;
using SCMM.Discord.Client;
using SCMM.Shared.Abstractions.Messaging;

namespace SCMM.Discord.Bot.Server.Handlers
{
    public class DiscordSendMessageHandler : IMessageHandler<SendDiscordMessage>
    {
        private readonly DiscordClient _client;

        public DiscordSendMessageHandler(DiscordClient client)
        {
            _client = client;
        }

        public Task HandleAsync(SendDiscordMessage message, IMessageContext context)
        {
            if (!String.IsNullOrEmpty(message.Username))
            {
                return _client.SendMessageAsync(
                    userIdOrName: message.Username,
                    message: message.Message,
                    title: message.Title,
                    description: message.Description,
                    fields: message.Fields,
                    fieldsInline: message.FieldsInline,
                    url: message.Url,
                    thumbnailUrl: message.ThumbnailUrl,
                    imageUrl: message.ImageUrl,
                    color: message.Colour
                );
            }
            else if (message.GuidId > 0 && message.ChannelId > 0)
            {
                return _client.SendMessageAsync(
                    guildId: message.GuidId.Value,
                    channelId: message.ChannelId.Value,
                    message: message.Message,
                    title: message.Title,
                    description: message.Description,
                    fields: message.Fields,
                    fieldsInline: message.FieldsInline,
                    url: message.Url,
                    thumbnailUrl: message.ThumbnailUrl,
                    imageUrl: message.ImageUrl,
                    color: message.Colour
                );
            }
            else
            {
                throw new Exception("Unable to send message, either the username or guild/channel must be set");
            }
        }
    }
}
