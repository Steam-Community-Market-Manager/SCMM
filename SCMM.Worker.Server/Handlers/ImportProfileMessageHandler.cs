using CommandQuery;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Abstractions.Messaging.Attributes;
using SCMM.Shared.API.Messages;
using SCMM.Steam.API.Commands;

namespace SCMM.Worker.Server.Handlers
{
    /*
    [Concurrency(MaxConcurrentCalls = 1)]
    public class ImportProfileMessageHandler : IMessageHandler<ImportProfileMessage>
    {
        private readonly ICommandProcessor _commandProcessor;

        public ImportProfileMessageHandler(ICommandProcessor commandProcessor)
        {
            _commandProcessor = commandProcessor;
        }

        public async Task HandleAsync(ImportProfileMessage message, IMessageContext context)
        {
            await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileRequest()
            {
                ProfileId = message.ProfileId,
                ImportFriendsListAsync = true,
                ImportInventoryAsync = true
            });
        }
    }
    */
}
