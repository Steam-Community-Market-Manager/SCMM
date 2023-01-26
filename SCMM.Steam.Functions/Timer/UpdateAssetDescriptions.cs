using CommandQuery;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.Functions.Timer;

public class UpdateAssetDescriptions
{
    private readonly SteamDbContext _db;
    private readonly ICommandProcessor _commandProcessor;

    public UpdateAssetDescriptions(ICommandProcessor commandProcessor, SteamDbContext db)
    {
        _commandProcessor = commandProcessor;
        _db = db;
    }

    [Function("Update-Asset-Descriptions")]
    public async Task Run([TimerTrigger("0 0/3 * * * *")] /* every 3 minutes */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Asset-Descriptions");

        var cutoff = DateTimeOffset.Now.Subtract(TimeSpan.FromHours(24));
        var assetDescriptions = await _db.SteamAssetDescriptions
            .Where(x => x.ClassId != null)
            .Where(x => x.TimeRefreshed == null || x.TimeRefreshed <= cutoff)
            .Where(x => x.App.IsActive)
            .OrderBy(x => x.TimeRefreshed)
            .Select(x => new
            {
                AppId = x.App.SteamId,
                ClassId = x.ClassId,
                Name = x.Name
            })
            .Take(30) // batch 30 at a time
            .ToListAsync();

        if (!assetDescriptions.Any())
        {
            return;
        }

        var id = Guid.NewGuid();
        logger.LogTrace($"Updating asset description information (id: {id}, count: {assetDescriptions.Count()})");
        foreach (var assetDescription in assetDescriptions)
        {
            try
            {
                await _commandProcessor.ProcessWithResultAsync(new ImportSteamAssetDescriptionRequest()
                {
                    AppId = ulong.Parse(assetDescription.AppId),
                    AssetClassId = assetDescription.ClassId.Value
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update asset description for '{assetDescription.ClassId}'. {ex.Message}");
                continue;
            }
        }

        await _db.SaveChangesAsync();

        logger.LogTrace($"Updated asset description information (id: {id})");
    }
}
