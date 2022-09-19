using CommandQuery;
using Microsoft.Azure.Functions.Worker;
using SCMM.Shared.API.Messages;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Data.Models;

namespace SCMM.Steam.Functions.ServiceBus;

public class ImportProfileInventoryMessageHandler
{
    private readonly ICommandProcessor _commandProcessor;

    public ImportProfileInventoryMessageHandler(ICommandProcessor commandProcessor)
    {
        _commandProcessor = commandProcessor;
    }

    [Function("Import-Profile-Inventory")]
    public async Task Run([ServiceBusTrigger("import-profile-inventory", Connection = "ServiceBusConnection")] ImportProfileInventoryMessage message, FunctionContext context)
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
