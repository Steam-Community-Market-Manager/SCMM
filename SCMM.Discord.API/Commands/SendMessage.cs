using CommandQuery;
using SCMM.Discord.API.Messages;
using SCMM.Shared.Abstractions.Messaging;

namespace SCMM.Discord.API.Commands
{
    public class SendMessageRequest : SendDiscordMessage, ICommand
    {
    }

    public class SendMessage : ICommandHandler<SendMessageRequest>
    {
        private readonly IServiceBus _serviceBus;

        public SendMessage(IServiceBus serviceBus)
        {
            _serviceBus = serviceBus;
        }

        public async Task HandleAsync(SendMessageRequest request)
        {
            await _serviceBus.SendMessageAsync(request);
        }
    }
}
