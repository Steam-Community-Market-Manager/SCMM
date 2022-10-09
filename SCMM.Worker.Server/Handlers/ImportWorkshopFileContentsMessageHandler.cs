using CommandQuery;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Abstractions.Messaging.Attributes;
using SCMM.Shared.API.Messages;
using SCMM.Steam.API.Commands;

namespace SCMM.Worker.Server.Handlers
{
    [Concurrency(MaxConcurrentCalls = 1)]
    public class ImportWorkshopFileContentsMessageHandler : IMessageHandler<ImportWorkshopFileContentsMessage>
    {
        private readonly ICommandProcessor _commandProcessor;

        public ImportWorkshopFileContentsMessageHandler(ICommandProcessor commandProcessor)
        {
            _commandProcessor = commandProcessor;
        }

        public async Task HandleAsync(ImportWorkshopFileContentsMessage message, IMessageContext context)
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
