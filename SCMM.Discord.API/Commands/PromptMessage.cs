using CommandQuery;
using SCMM.Azure.ServiceBus;
using SCMM.Discord.API.Messages;

namespace SCMM.Discord.API.Commands
{
    public class PromptMessageRequest : DiscordPromptMessage, ICommand<PromptMessageResponse>
    {
    }

    public class PromptMessageResponse : DiscordPromptMessageReply
    {
    }

    public class PromptMessage : ICommandHandler<PromptMessageRequest, PromptMessageResponse>
    {
        private readonly ServiceBusClient _client;

        public PromptMessage(ServiceBusClient client)
        {
            _client = client;
        }

        public async Task<PromptMessageResponse> HandleAsync(PromptMessageRequest request, CancellationToken cancellationToken)
        {
            return await _client.SendMessageAndAwaitReplyAsync<PromptMessageRequest, PromptMessageResponse>(request);
        }
    }
}
