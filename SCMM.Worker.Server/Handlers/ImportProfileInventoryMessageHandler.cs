using CommandQuery;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Abstractions.Messaging.Attributes;
using SCMM.Shared.API.Messages;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Data.Models;

namespace SCMM.Worker.Server.Handlers
{
    [Concurrency(MaxConcurrentCalls = 1)]
    public class ImportProfileInventoryMessageHandler : IMessageHandler<ImportProfileInventoryMessage>
    {
        private readonly ICommandProcessor _commandProcessor;

        public ImportProfileInventoryMessageHandler(ICommandProcessor commandProcessor)
        {
            _commandProcessor = commandProcessor;
        }

        public async Task HandleAsync(ImportProfileInventoryMessage message, IMessageContext context)
        {
            var importResult = await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileInventoryRequest()
            {
                ProfileId = message.ProfileId,
                AppIds = message.AppIds
            });

            var appIds = importResult?.Profile?.InventoryItems?.Select(x => x.App?.SteamId)?.Distinct()?.ToArray();
            if (appIds != null)
            {
                foreach (var appId in appIds)
                {
                    await _commandProcessor.ProcessWithResultAsync(new CalculateSteamProfileInventoryTotalsRequest()
                    {
                        ProfileId = message.ProfileId,
                        AppId = appId,
                        CurrencyId = Constants.SteamDefaultCurrency
                    });
                }
            }
        }
    }
}
