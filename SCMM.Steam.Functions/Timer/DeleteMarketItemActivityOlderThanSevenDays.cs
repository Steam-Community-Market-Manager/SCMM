using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.Functions.Timer;

public class DeleteMarketItemActivityOlderThanSevenDays
{
    private readonly SteamDbContext _db;

    public DeleteMarketItemActivityOlderThanSevenDays(SteamDbContext db)
    {
        _db = db;
    }

    [Function("Delete-Market-Item-Activity-Older-Than-Seven-Days")]
    public async Task Run([TimerTrigger("0 0 0 * * *")] /* every day at midnight */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Delete-Market-Item-Activity-Older-Than-Seven-Days");

        // Delete all market activity older than 7 days
        var cutoffDate = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(7));
        var deletedActivity = await _db.SteamMarketItemActivity
            .Where(x => x.Timestamp < cutoffDate)
            .ExecuteDeleteAsync();

        logger.LogTrace($"Deleted {deletedActivity} market item activities");
    }
}
