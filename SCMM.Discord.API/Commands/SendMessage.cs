using CommandQuery;
using SCMM.Azure.ServiceBus;
using SCMM.Discord.API.Messages;

namespace SCMM.Discord.API.Commands
{
    public class SendMessageRequest : DiscordSendMessage, ICommand
    {
    }

    public class SendMessage : ICommandHandler<SendMessageRequest>
    {
        private readonly ServiceBusClient _client;

        public SendMessage(ServiceBusClient client)
        {
            _client = client;
        }

        public async Task HandleAsync(SendMessageRequest request)
        {
            await _client.SendMessageAsync(request);
        }
    }
}
