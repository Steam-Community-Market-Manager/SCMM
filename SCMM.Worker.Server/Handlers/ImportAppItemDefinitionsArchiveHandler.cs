using CommandQuery;
using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;
using SCMM.Shared.API.Messages;
using SCMM.Steam.API.Commands;

namespace SCMM.Worker.Server.Handlers
{
    [Concurrency(MaxConcurrentCalls = 1)]
    public class ImportAppItemDefinitionsArchiveHandler : IMessageHandler<ImportAppItemDefinitionsArchiveMessage>
    {
        private readonly ICommandProcessor _commandProcessor;

        public ImportAppItemDefinitionsArchiveHandler(ICommandProcessor commandProcessor)
        {
            _commandProcessor = commandProcessor;
        }

        public async Task HandleAsync(ImportAppItemDefinitionsArchiveMessage message, MessageContext context)
        {
            await _commandProcessor.ProcessAsync(new ImportSteamAppItemDefinitionsArchiveRequest()
            {
                AppId = message.AppId,
                ItemDefinitionsDigest = message.ItemDefinitionsDigest
            });
        }
    }
}
