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
