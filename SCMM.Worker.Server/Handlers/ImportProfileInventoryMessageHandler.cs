using CommandQuery;
using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;
using SCMM.Shared.API.Messages;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Data.Models;

namespace SCMM.Worker.Server.Handlers
{
    [Concurrency(MaxConcurrentCalls = 1)]
    public class ImportProfileInventoryMessageHandler : Worker.Client.WebClient, IMessageHandler<ImportProfileInventoryMessage>
    {
        private readonly ICommandProcessor _commandProcessor;

        public ImportProfileInventoryMessageHandler(ICommandProcessor commandProcessor)
        {
            _commandProcessor = commandProcessor;
        }

        public async Task HandleAsync(ImportProfileInventoryMessage message, MessageContext context)
        {
            await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileInventoryRequest()
            {
                AppId = message.AppId.ToString(),
                ProfileId = message.ProfileId
            });
            await _commandProcessor.ProcessWithResultAsync(new CalculateSteamProfileInventoryTotalsRequest()
            {
                AppId = message.AppId.ToString(),
                ProfileId = message.ProfileId,
                CurrencyId = Constants.SteamDefaultCurrency
            });
        }
    }
}
