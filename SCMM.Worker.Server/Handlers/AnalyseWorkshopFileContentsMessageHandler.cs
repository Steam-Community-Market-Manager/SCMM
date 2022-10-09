using CommandQuery;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Abstractions.Messaging.Attributes;
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

        public async Task HandleAsync(AnalyseWorkshopFileContentsMessage message, IMessageContext context)
        {
            await _commandProcessor.ProcessAsync(new AnalyseSteamWorkshopContentsInBlobStorageRequest()
            {
                BlobName = message.BlobName,
                Force = message.Force
            });
        }
    }
}
