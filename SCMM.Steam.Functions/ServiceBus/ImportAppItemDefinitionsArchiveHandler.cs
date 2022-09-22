using CommandQuery;
using Microsoft.Azure.Functions.Worker;
using SCMM.Shared.API.Messages;
using SCMM.Steam.API.Commands;

namespace SCMM.Steam.Functions.ServiceBus;

public class ImportAppItemDefinitionsArchiveHandler
{
    private readonly ICommandProcessor _commandProcessor;

    public ImportAppItemDefinitionsArchiveHandler(ICommandProcessor commandProcessor)
    {
        _commandProcessor = commandProcessor;
    }

    [Function("Import-App-Item-Definitions-Archive")]
    public async Task Run([ServiceBusTrigger("import-app-item-definitions-archive", Connection = "ServiceBusConnection")] ImportAppItemDefinitionsArchiveMessage message, FunctionContext context)
    {
        await _commandProcessor.ProcessAsync(new ImportSteamAppItemDefinitionsArchiveRequest()
        {
            AppId = message.AppId,
            ItemDefinitionsDigest = message.ItemDefinitionsDigest,
            ParseChanges = message.ParseChanges
        });
    }
}
