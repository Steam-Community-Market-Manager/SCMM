using Azure.Messaging.ServiceBus;
using CommandQuery;
using SCMM.Azure.ServiceBus.Extensions;
using SCMM.Discord.API.Messages;
using System;
using System.Threading.Tasks;

namespace SCMM.Discord.API.Commands
{
    public class SendDiscordMessageRequest : DiscordNotificationMessage, ICommand
    {
    }

    public class SendDiscordMessage : ICommandHandler<SendDiscordMessageRequest>
    {
        private readonly ServiceBusClient _client;

        public SendDiscordMessage(ServiceBusClient client)
        {
            _client = client;
        }

        public async Task HandleAsync(SendDiscordMessageRequest request)
        {
            await using var sender = _client.CreateSender<SendDiscordMessageRequest>();
            await sender.SendMessageAsync(
                new ServiceBusMessage(BinaryData.FromObjectAsJson(request))
            );
        }
    }
}
