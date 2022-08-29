using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;
using SCMM.Discord.API.Messages;
using SCMM.Discord.Client;

namespace SCMM.Discord.Bot.Server.Handlers
{
    [Concurrency(MaxConcurrentCalls = 10)]
    public class DiscordSendMessageHandler : IMessageHandler<SendDiscordMessage>
    {
        private readonly DiscordClient _client;

        public DiscordSendMessageHandler(DiscordClient client)
        {
            _client = client;
        }

        public Task HandleAsync(SendDiscordMessage message, MessageContext context)
        {
            if (!String.IsNullOrEmpty(message.Username))
            {
                return _client.SendMessageAsync(
                    idOrUsername: message.Username,
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
                return _client.SendMessageAsync(
                    guildId: message.GuidId,
                    channelPatterns: message.ChannelPatterns,
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
        }
    }
}
