using CommandQuery;
using SCMM.Discord.API.Messages;
using SCMM.Shared.Abstractions.Messaging;

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
        private readonly IServiceBus _serviceBus;

        public PromptMessage(IServiceBus serviceBus)
        {
            _serviceBus = serviceBus;
        }

        public async Task<PromptMessageResponse> HandleAsync(PromptMessageRequest request)
        {
            return await _serviceBus.SendMessageAndAwaitReplyAsync<PromptDiscordMessage, PromptMessageResponse>(request);
        }
    }
}
