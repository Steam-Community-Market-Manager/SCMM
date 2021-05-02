using SCMM.Discord.API.Commands;
using SCMM.Discord.Client;
using SCMM.Shared.Azure.ServiceBus;
using SCMM.Shared.Azure.ServiceBus.Attributes;
using System.Drawing;
using System.Threading.Tasks;

namespace SCMM.Discord.Bot.Server.Handlers
{
    [Concurrency(MaxConcurrentCalls = 10)]
    public class BroadcastNotificationHandler : IMessageHandler<BroadcastNotificationRequest>
    {
        private readonly DiscordClient _client;

        public BroadcastNotificationHandler( DiscordClient client)
        {
            _client = client;
        }

        public Task HandleAsync(BroadcastNotificationRequest message)
        {
            return _client.BroadcastMessageAsync(
                guildPattern: message.GuildPattern,
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
