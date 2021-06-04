using SCMM.Shared.Data.Models.Extensions;
using System;

namespace SCMM.Web.Data.Models.Extensions
{
    public static class SteamExtensions
    {
        public static string ToMarketAgeString(this TimeSpan? timespan)
        {
            if (timespan == null)
            {
                return null;
            }
            return timespan.Value.ToDurationString(
                showWeeks: false,
                showHours: false,
                showMinutes: false,
                showSeconds: false,
                maxGranularity: 2
            );
        }
    }
}
