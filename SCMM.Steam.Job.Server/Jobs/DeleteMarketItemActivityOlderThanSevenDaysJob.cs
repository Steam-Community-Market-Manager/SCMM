using Microsoft.EntityFrameworkCore;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Job.Server.Attributes;

namespace SCMM.Steam.Job.Server.Jobs;

[Job("Delete-Market-Item-Activity-Older-Than-Seven-Days", "0 0 * * *")]
public class DeleteMarketItemActivityOlderThanSevenDaysJob : InvocableJob
{
    private readonly ILogger<DeleteMarketItemActivityOlderThanSevenDaysJob> _logger;
    private readonly SteamDbContext _db;

    public DeleteMarketItemActivityOlderThanSevenDaysJob(ILogger<DeleteMarketItemActivityOlderThanSevenDaysJob> logger, SteamDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public override async Task Run(CancellationToken cancellationToken)
    {
        // Delete all market activity older than 7 days
        var cutoffDate = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(7));
        var deletedActivity = await _db.SteamMarketItemActivity
            .Where(x => x.Timestamp < cutoffDate)
            .ExecuteDeleteAsync();

        _logger.LogTrace($"Deleted {deletedActivity} market item activities");
    }
}
