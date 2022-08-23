using CommandQuery;
using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.API.Queries
{
    public class GetStoreNextUpdateTimeRequest : IQuery<GetStoreNextUpdateTimeResponse>
    {
        public ulong AppId { get; set; }
    }

    public class GetStoreNextUpdateTimeResponse
    {
        public DateTimeOffset Timestamp { get; set; }

        public TimeSpan TimeRemaining { get; set; }

        public string TimeDescription { get; set; }

        public bool IsOverdue { get; set; }
    }

    public class GetStoreNextUpdateTime : IQueryHandler<GetStoreNextUpdateTimeRequest, GetStoreNextUpdateTimeResponse>
    {
        private readonly SteamDbContext _db;

        public GetStoreNextUpdateTime(SteamDbContext db)
        {
            _db = db;
        }

        public async Task<GetStoreNextUpdateTimeResponse> HandleAsync(GetStoreNextUpdateTimeRequest request, CancellationToken cancellationToken)
        {
            var recentStoreStarts = await _db.SteamItemStores
                .Where(x => x.App.SteamId == request.AppId.ToString())
                .Where(x => x.Start != null)
                .OrderByDescending(x => x.Start).Take(5)
                .Select(x => x.Start.Value)
                .ToListAsync();

            if (recentStoreStarts.Any() != true)
            {
                return null;
            }

            // Average out the last month or so of store start times
            // For reference, the store normally updates around Friday 6am NZT (Thursday 5pm UTC)
            var lastStoreStart = recentStoreStarts.FirstOrDefault();
            var averageStoreStartTime = TimeSpan.FromSeconds(recentStoreStarts.Select(s => s.TimeOfDay.TotalSeconds).Average());
            var nextStoreStartUtc = (lastStoreStart.UtcDateTime.Date + averageStoreStartTime);
            do
            {
                nextStoreStartUtc = nextStoreStartUtc.AddDays(1);
            } while (nextStoreStartUtc.DayOfWeek != DayOfWeek.Thursday);

            // If the store is overdue by more than 6hrs, assume it will update the next day at the same time
            while ((nextStoreStartUtc + TimeSpan.FromHours(6)) <= DateTime.UtcNow)
            {
                nextStoreStartUtc = nextStoreStartUtc.AddDays(1);
            }

            var nextStoreUpdate = new DateTimeOffset(nextStoreStartUtc, TimeZoneInfo.Utc.BaseUtcOffset);
            var nextStoreRemaining = (nextStoreUpdate - DateTimeOffset.Now);
            var nextStoreIsOverdue = (nextStoreUpdate <= DateTimeOffset.Now);
            return new GetStoreNextUpdateTimeResponse
            {
                Timestamp = nextStoreUpdate,
                TimeRemaining = nextStoreRemaining.Duration(),
                TimeDescription = nextStoreRemaining.Duration().ToDurationString(
                    prefix: (nextStoreIsOverdue ? "overdue by about" : "expected in roughly"), zero: "expected anytime now", showSeconds: false, maxGranularity: 2
                ),
                IsOverdue = nextStoreIsOverdue
            };
        }
    }
}
