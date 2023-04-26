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
    public async Task Run([TimerTrigger("0 0/15 * * * *")] /* every 15 minutes */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Asset-Descriptions");

        var cutoff = DateTimeOffset.Now.Subtract(TimeSpan.FromHours(23));
        var assetDescriptions = await _db.SteamAssetDescriptions
            .Where(x => x.ClassId != null)
            .Where(x => x.App.IsActive)
            .Where(x => x.TimeRefreshed == null || x.TimeRefreshed <= cutoff)
            .OrderBy(x => x.TimeRefreshed)
            .Select(x => new
            {
                AppId = x.App.SteamId,
                ClassId = x.ClassId
            })
            .Take(300) // batch 300 at a time
            .ToListAsync();

        if (!assetDescriptions.Any())
        {
            return;
        }

        logger.LogTrace($"Updating asset description information (count: {assetDescriptions.Count()})");
        foreach (var assetDescriptionGroup in assetDescriptions.GroupBy(x => x.AppId))
        {
            try
            {
                await _commandProcessor.ProcessWithResultAsync(new ImportSteamAssetDescriptionsRequest()
                {
                    AppId = ulong.Parse(assetDescriptionGroup.Key),
                    AssetClassIds = assetDescriptionGroup
                        .Select(x => x.ClassId ?? 0)
                        .Where(x => x > 0)
                        .ToArray()
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update asset descriptions. {ex.Message}");
                continue;
            }
            finally
            {
                await _db.SaveChangesAsync();
            }
        }

        logger.LogTrace($"Updated asset description information");
    }
}
