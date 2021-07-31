using CommandQuery;
using SCMM.Azure.ServiceBus;
using SCMM.Discord.API.Messages;
using System.Threading.Tasks;

namespace SCMM.Discord.API.Commands
{
    public class PromptDiscordMessageRequest : DiscordPromptMessage, ICommand<PromptDiscordMessageResponse>
    {
    }

    public class PromptDiscordMessageResponse : DiscordPromptReplyMessage
    {
    }

    public class PromptDiscordMessage : ICommandHandler<PromptDiscordMessageRequest, PromptDiscordMessageResponse>
    {
        private readonly ServiceBusClient _client;

        public PromptDiscordMessage(ServiceBusClient client)
        {
            _client = client;
        }

        public async Task<PromptDiscordMessageResponse> HandleAsync(PromptDiscordMessageRequest request)
        {
            return await _client.SendMessageAndAwaitReplyAsync<PromptDiscordMessageRequest, PromptDiscordMessageResponse>(request);
        }
    }
}
