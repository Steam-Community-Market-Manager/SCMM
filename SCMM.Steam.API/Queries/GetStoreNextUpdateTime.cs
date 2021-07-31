using AutoMapper;
using CommandQuery;
using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Steam.API.Queries
{
    public class GetStoreNextUpdateTimeRequest : IQuery<GetStoreNextUpdateTimeResponse>
    {
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
        private readonly IMapper _mapper;

        public GetStoreNextUpdateTime(SteamDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<GetStoreNextUpdateTimeResponse> HandleAsync(GetStoreNextUpdateTimeRequest request, CancellationToken cancellationToken)
        {
            var lastItemAcceptedOnQuery = await _db.SteamAssetDescriptions
                .AsNoTracking()
                .Where(x => x.TimeAccepted != null)
                .MaxAsync(x => x.TimeAccepted);

            var lastItemAcceptedOn = lastItemAcceptedOnQuery?.UtcDateTime;
            if (lastItemAcceptedOn == null)
            {
                return null;
            }

            // Store normally updates around Friday 6am NZT (Thursday 5pm UTC)
            var nextStoreUpdateUtc = (lastItemAcceptedOn.Value.Date + new TimeSpan(17, 0, 0));
            do
            {
                nextStoreUpdateUtc = nextStoreUpdateUtc.AddDays(1);
            } while (nextStoreUpdateUtc.DayOfWeek != DayOfWeek.Thursday);

            // If the store is overdue by more than 6hrs, assume it will update the next day at the same time
            while ((nextStoreUpdateUtc + TimeSpan.FromHours(6)) <= DateTime.UtcNow)
            {
                nextStoreUpdateUtc = nextStoreUpdateUtc.AddDays(1);
            }

            var nextStoreUpdate = new DateTimeOffset(nextStoreUpdateUtc, TimeZoneInfo.Utc.BaseUtcOffset);
            var nextStoreRemaining = (nextStoreUpdate - DateTimeOffset.Now);
            var nextStoreIsOverdue = (nextStoreUpdate <= DateTimeOffset.Now);
            return new GetStoreNextUpdateTimeResponse
            {
                Timestamp = nextStoreUpdate,
                TimeRemaining = nextStoreRemaining,
                TimeDescription = nextStoreRemaining.ToDurationString(
                    prefix: (nextStoreIsOverdue ? "overdue by" : "due in"), zero: "due now", showSeconds: false, maxGranularity: 2
                ),
                IsOverdue = nextStoreIsOverdue
            };
        }
    }
}
