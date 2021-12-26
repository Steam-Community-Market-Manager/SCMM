using CommandQuery;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.Functions.Timer;

public class UpdateAssetDescriptionsJob
{
    private readonly SteamDbContext _db;
    private readonly ICommandProcessor _commandProcessor;

    public UpdateAssetDescriptionsJob(ICommandProcessor commandProcessor, SteamDbContext db)
    {
        _commandProcessor = commandProcessor;
        _db = db;
    }

    [Function("Update-Asset-Descriptions")]
    public async Task Run([TimerTrigger("0 0/15 * * * *")] /* every 15 minutes */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Asset-Descriptions");

        var cutoff = DateTimeOffset.Now.Subtract(TimeSpan.FromHours(24));
        var assetDescriptions = _db.SteamAssetDescriptions
            .Where(x => x.TimeRefreshed == null || x.TimeRefreshed <= cutoff)
            .OrderBy(x => x.TimeRefreshed)
            .Select(x => new
            {
                AppId = x.App.SteamId,
                x.ClassId
            })
            .Take(30) // batch 30 at a time
            .ToList();

        if (!assetDescriptions.Any())
        {
            return;
        }

        var id = Guid.NewGuid();
        logger.LogInformation($"Updating asset description information (id: {id}, count: {assetDescriptions.Count()})");
        foreach (var assetDescription in assetDescriptions)
        {
            try
            {
                await _commandProcessor.ProcessWithResultAsync(new ImportSteamAssetDescriptionRequest()
                {
                    AppId = ulong.Parse(assetDescription.AppId),
                    AssetClassId = assetDescription.ClassId
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update asset description for '{assetDescription.ClassId}'. {ex.Message}");
                continue;
            }
        }

        _db.SaveChanges();
        logger.LogInformation($"Updated asset description information (id: {id})");
    }
}
