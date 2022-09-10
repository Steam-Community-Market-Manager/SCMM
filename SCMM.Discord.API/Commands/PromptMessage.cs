using CommandQuery;
using SCMM.Azure.ServiceBus;
using SCMM.Discord.API.Messages;

namespace SCMM.Discord.API.Commands
{
    public class PromptMessageRequest : PromptDiscordMessage, ICommand<PromptMessageResponse>
    {
    }

    public class PromptMessageResponse : PromptDiscordMessage.Reply
    {
    }

    public class PromptMessage : ICommandHandler<PromptMessageRequest, PromptMessageResponse>
    {
        private readonly ServiceBusClient _client;

        public PromptMessage(ServiceBusClient client)
        {
            _client = client;
        }

        public async Task<PromptMessageResponse> HandleAsync(PromptMessageRequest request)
        {
            return await _client.SendMessageAndAwaitReplyAsync<PromptDiscordMessage, PromptMessageResponse>(request);
        }
    }
}
