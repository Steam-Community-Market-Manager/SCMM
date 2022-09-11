using CommandQuery;
using Microsoft.Azure.Functions.Worker;
using SCMM.Steam.API.Commands;
using SCMM.Steam.API.Messages;

namespace SCMM.Steam.Functions.ServiceBus;

public class AnalyseWorkshopFileContentsHandler
{
    private readonly ICommandProcessor _commandProcessor;

    public AnalyseWorkshopFileContentsHandler(ICommandProcessor commandProcessor)
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
