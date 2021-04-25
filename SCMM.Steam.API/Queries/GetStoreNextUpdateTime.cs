using AutoMapper;
using CommandQuery;
using Microsoft.EntityFrameworkCore;
using SCMM.Data.Shared.Extensions;
using SCMM.Steam.Data.Store;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Queries
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

        public async Task<GetStoreNextUpdateTimeResponse> HandleAsync(GetStoreNextUpdateTimeRequest request)
        {
            var lastItemAcceptedOnQuery = await _db.SteamAssetWorkshopFiles
                .AsNoTracking()
                .Where(x => x.AcceptedOn != null)
                .GroupBy(x => 1)
                .Select(x => x.Max(y => y.AcceptedOn))
                .FirstOrDefaultAsync();

            var lastItemAcceptedOn = lastItemAcceptedOnQuery?.UtcDateTime;
            if (lastItemAcceptedOn == null)
            {
                return null;
            }

            // Store normally updates around Friday 12pm NZT (Thursday 11pm UTC)
            var nextStoreUpdateUtc = (lastItemAcceptedOn.Value.Date + new TimeSpan(23, 0, 0));
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
