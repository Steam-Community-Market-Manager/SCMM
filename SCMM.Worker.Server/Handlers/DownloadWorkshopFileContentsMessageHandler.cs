using CommandQuery;
using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;
using SCMM.Shared.API.Messages;
using SCMM.Steam.API.Commands;

namespace SCMM.Worker.Server.Handlers
{
    [Concurrency(MaxConcurrentCalls = 1)]
    public class DownloadWorkshopFileContentsMessageHandler : Worker.Client.WebClient, IMessageHandler<DownloadWorkshopFileContentsMessage>
    {
        private readonly ICommandProcessor _commandProcessor;

        public DownloadWorkshopFileContentsMessageHandler(ICommandProcessor commandProcessor)
        {
            _commandProcessor = commandProcessor;
        }

        public async Task HandleAsync(DownloadWorkshopFileContentsMessage message, MessageContext context)
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
