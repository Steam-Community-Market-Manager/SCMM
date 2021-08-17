using CommandQuery;
using SCMM.Azure.ServiceBus;
using SCMM.Discord.API.Messages;

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
            await _client.SendMessageAsync(request);
        }
    }
}
