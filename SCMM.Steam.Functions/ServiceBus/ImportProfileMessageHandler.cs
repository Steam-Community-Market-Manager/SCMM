using CommandQuery;
using Microsoft.Azure.Functions.Worker;
using SCMM.Shared.API.Messages;
using SCMM.Steam.API.Commands;

namespace SCMM.Worker.Server.Handlers;

public class ImportProfileMessageHandler
{
    private readonly ICommandProcessor _commandProcessor;

    public ImportProfileMessageHandler(ICommandProcessor commandProcessor)
    {
        _commandProcessor = commandProcessor;
    }

    [Function("Import-Profile")]
    public async Task Run([ServiceBusTrigger("import-profile", Connection = "ServiceBusConnection")] ImportProfileMessage message, FunctionContext context)
    {
        await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileRequest()
        {
            ProfileId = message.ProfileId,
            ImportFriendsListAsync = true,
            ImportInventoryAsync = true
        });
    }
}
