using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;
using SCMM.Discord.API.Messages;
using SCMM.Discord.Client;
using System.Drawing;

namespace SCMM.Discord.Bot.Server.Handlers
{
    [Concurrency(MaxConcurrentCalls = 10)]
    public class DiscordNotificationMessageHandler : IMessageHandler<DiscordNotificationMessage>
    {
        private readonly DiscordClient _client;

        public DiscordNotificationMessageHandler(DiscordClient client)
        {
            _client = client;
        }

        public Task HandleAsync(DiscordNotificationMessage message, MessageContext context)
        {
            return _client.SendMessageAsync(
                guildId: message.GuidId,
                channelPattern: message.ChannelPattern,
                message: message.Message,
                title: message.Title,
                description: message.Description,
                fields: message.Fields,
                fieldsInline: message.FieldsInline,
                url: message.Url,
                thumbnailUrl: message.ThumbnailUrl,
                imageUrl: message.ImageUrl,
                color: ColorTranslator.FromHtml(message.Colour)
            );
        }
    }
}
