using CommandQuery;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Abstractions.Messaging.Attributes;
using SCMM.Shared.API.Messages;
using SCMM.Steam.API.Commands;

namespace SCMM.Worker.Server.Handlers
{
    [Concurrency(MaxConcurrentCalls = 1)]
    public class ImportProfileFriendsMessageHandler : IMessageHandler<ImportProfileFriendsMessage>
    {
        private readonly ICommandProcessor _commandProcessor;

        public ImportProfileFriendsMessageHandler(ICommandProcessor commandProcessor)
        {
            _commandProcessor = commandProcessor;
        }

        public async Task HandleAsync(ImportProfileFriendsMessage message, IMessageContext context)
        {
            await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileFriendsRequest()
            {
                ProfileId = message.ProfileId,
                ImportInventoryAsync = true
            });
        }
    }
}
