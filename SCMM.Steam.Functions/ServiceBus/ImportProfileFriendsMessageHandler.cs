using CommandQuery;
using Microsoft.Azure.Functions.Worker;
using SCMM.Shared.API.Messages;
using SCMM.Steam.API.Commands;

namespace SCMM.Worker.Server.Handlers;

public class ImportProfileFriendsMessageHandler
{
    private readonly ICommandProcessor _commandProcessor;

    public ImportProfileFriendsMessageHandler(ICommandProcessor commandProcessor)
    {
        _commandProcessor = commandProcessor;
    }

    [Function("Import-Profile-Friends")]
    public async Task Run([ServiceBusTrigger("import-profile-friends", Connection = "ServiceBusConnection")] ImportProfileFriendsMessage message, FunctionContext context)
    {
        await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileFriendsRequest()
        {
            ProfileId = message.ProfileId,
            ImportInventoryAsync = true
        });
    }
}
