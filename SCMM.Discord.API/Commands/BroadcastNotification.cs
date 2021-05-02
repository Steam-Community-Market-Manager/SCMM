using Azure.Messaging.ServiceBus;
using CommandQuery;
using SCMM.Shared.Azure.ServiceBus;
using SCMM.Shared.Azure.ServiceBus.Attributes;
using SCMM.Shared.Azure.ServiceBus.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace SCMM.Discord.API.Commands
{
    [Queue(Name = "DiscordNotifications")]
    public class BroadcastNotificationRequest : ICommand, IMessage
    {
        public string GuildPattern { get; set; }

        public string ChannelPattern { get; set; }

        public string Message { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public IDictionary<string, string> Fields { get; set; }

        public bool FieldsInline { get; set; }

        public string Url { get; set; }

        public string ThumbnailUrl { get; set; }

        public string ImageUrl { get; set; }

        public string Colour { get; set; }
    }

    public class BroadcastNotification : ICommandHandler<BroadcastNotificationRequest>
    {
        private readonly ServiceBusClient _client;

        public BroadcastNotification(ServiceBusClient client)
        {
            _client = client;
        }

        public async Task HandleAsync(BroadcastNotificationRequest request)
        {
            await using (var sender = _client.CreateSender<BroadcastNotificationRequest>())
            {
                await sender.SendMessageAsync(
                    new ServiceBusMessage(BinaryData.FromObjectAsJson(request))
                );
            }
        }
    }
}
