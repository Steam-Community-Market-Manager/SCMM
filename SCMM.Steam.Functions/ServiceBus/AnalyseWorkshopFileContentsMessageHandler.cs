using CommandQuery;
using Microsoft.Azure.Functions.Worker;
using SCMM.Shared.API.Messages;
using SCMM.Steam.API.Commands;

namespace SCMM.Steam.Functions.ServiceBus;

public class AnalyseWorkshopFileContentsMessageHandler
{
    private readonly ICommandProcessor _commandProcessor;

    public AnalyseWorkshopFileContentsMessageHandler(ICommandProcessor commandProcessor)
    {
        _commandProcessor = commandProcessor;
    }

    [Function("Analyse-Workshop-File-Contents")]
    public async Task Run([ServiceBusTrigger("analyse-workshop-file-contents", Connection = "ServiceBusConnection")] AnalyseWorkshopFileContentsMessage message, FunctionContext context)
    {
        await _commandProcessor.ProcessAsync(new AnalyseSteamWorkshopContentsInBlobStorageRequest()
        {
            BlobName = message.BlobName,
            Force = message.Force
        });
    }
}
