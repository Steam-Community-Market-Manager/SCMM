using SCMM.Shared.Data.Models.Extensions;

namespace SCMM.Web.Data.Models.Extensions
{
    public static class UIExtensions
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
                maxGranularity: 2,
                zero: "Today"
            );
        }
    }
}
