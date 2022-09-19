using CommandQuery;
using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;
using SCMM.Shared.API.Messages;
using SCMM.Steam.API.Commands;

namespace SCMM.Worker.Server.Handlers
{
    [Concurrency(MaxConcurrentCalls = 1)]
    public class ImportWorkshopFileContentsMessageHandler : Worker.Client.WebClient, IMessageHandler<ImportWorkshopFileContentsMessage>
    {
        private readonly ICommandProcessor _commandProcessor;

        public ImportWorkshopFileContentsMessageHandler(ICommandProcessor commandProcessor)
        {
            _commandProcessor = commandProcessor;
        }

        public async Task HandleAsync(ImportWorkshopFileContentsMessage message, MessageContext context)
        {
            await _commandProcessor.ProcessWithResultAsync(new ImportSteamWorkshopFileToBlobStorageRequest()
            {
                AppId = message.AppId,
                PublishedFileId = message.PublishedFileId,
                Force = message.Force
            });
        }
    }
}
