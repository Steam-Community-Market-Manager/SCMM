using CommandQuery;
using SCMM.Azure.ServiceBus;
using SCMM.Discord.API.Messages;

namespace SCMM.Discord.API.Commands
{
    public class GetMessagesRequest : DiscordGetMessages, IQuery<GetMessagesResponse>
    {
    }

    public class GetMessagesResponse : DiscordGetMessagesReply
    {
    }

    public class GetMessages : IQueryHandler<GetMessagesRequest, GetMessagesResponse>
    {
        private readonly ServiceBusClient _client;

        public GetMessages(ServiceBusClient client)
        {
            _client = client;
        }

        public async Task<GetMessagesResponse> HandleAsync(GetMessagesRequest request, CancellationToken cancellationToken)
        {
            return await _client.SendMessageAndAwaitReplyAsync<GetMessagesRequest, GetMessagesResponse>(request);
        }
    }
}
