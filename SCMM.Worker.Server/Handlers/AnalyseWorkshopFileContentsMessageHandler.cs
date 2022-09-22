using CommandQuery;
using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;
using SCMM.Shared.API.Messages;
using SCMM.Steam.API.Commands;

namespace SCMM.Worker.Server.Handlers
{
    [Concurrency(MaxConcurrentCalls = 1)]
    public class AnalyseWorkshopFileContentsMessageHandler : IMessageHandler<AnalyseWorkshopFileContentsMessage>
    {
        private readonly ICommandProcessor _commandProcessor;

        public AnalyseWorkshopFileContentsMessageHandler(ICommandProcessor commandProcessor)
        {
            _commandProcessor = commandProcessor;
        }

        public async Task HandleAsync(AnalyseWorkshopFileContentsMessage message, MessageContext context)
        {
            await _commandProcessor.ProcessAsync(new AnalyseSteamWorkshopContentsInBlobStorageRequest()
            {
                BlobName = message.BlobName,
                Force = message.Force
            });
        }
    }
}
