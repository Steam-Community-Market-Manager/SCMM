using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public static IEnumerable<KeyValuePair<string, string>> WithoutWorkshopTags(this IDictionary<string, string> tags)
        {
            return tags?.Where(x => !x.Key.StartsWith(Constants.SteamAssetTagWorkshop));
        }
    }
}
