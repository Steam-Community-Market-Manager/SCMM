using CommandQuery;
using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;
using SCMM.Shared.API.Messages;
using SCMM.Steam.API.Commands;

namespace SCMM.Worker.Server.Handlers
{
    [Concurrency(MaxConcurrentCalls = 1)]
    public class ImportProfileMessageHandler : Worker.Client.WebClient, IMessageHandler<ImportProfileMessage>
    {
        private readonly ICommandProcessor _commandProcessor;

        public ImportProfileMessageHandler(ICommandProcessor commandProcessor)
        {
            _commandProcessor = commandProcessor;
        }

        public async Task HandleAsync(ImportProfileMessage message, MessageContext context)
        {
            await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileRequest()
            {
                ProfileId = message.ProfileId,
                ImportFriendsListAsync = true,
                ImportInventoryAsync = true
            });
        }
    }
}
