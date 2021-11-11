using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.Functions;

public class DeleteExpiredFileData
{
    private readonly SteamDbContext _db;

    public DeleteExpiredFileData(SteamDbContext db)
    {
        _db = db;
    }

    [Function("Delete-Expired-File-Data")]
    public async Task Run([TimerTrigger("0 0/15 * * * *")] /* every 15mins */ object timer, FunctionContext context)
    {
        var logger = context.GetLogger("Delete-Expired-File-Data");

        // Delete all files that have expired
        var now = DateTimeOffset.Now;
        var expiredFileData = await _db.FileData
            .Where(x => x.ExpiresOn != null && x.ExpiresOn <= now)
            .OrderByDescending(x => x.ExpiresOn)
            .Take(100) // batch 100 at a time to avoid timing out
            .ToListAsync();

        if (expiredFileData?.Any() == true)
        {
            logger.LogInformation($"Found {expiredFileData.Count}+ file data records that have expired, deleting...");
            foreach (var batch in expiredFileData.Batch(10))
            {
                _db.FileData.RemoveRange(batch);
                _db.SaveChanges();
            }

            logger.LogInformation($"{expiredFileData.Count} file data records were deleted");
        }
        else
        {
            logger.LogInformation($"No expired file data found");
        }
    }
}
