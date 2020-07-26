using SCMM.Steam.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SCMM.Web.Shared
{
    public static class SteamExtensions
    {
        public static IEnumerable<KeyValuePair<string, double>> ToGraphDictionary(this IDictionary<DateTime, double> graph)
        {
            return graph?.ToDictionary(
                x => x.Key.ToString("dd MMM yyyy"),
                x => x.Value
            );
        }

        public static IEnumerable<KeyValuePair<string, string>> WithoutWorkshopTags(this IDictionary<string, string> tags)
        {
            return tags?.Where(x => !x.Key.StartsWith(SteamConstants.SteamAssetTagWorkshop));
        }

        public static string ToMarketAgeString(this TimeSpan? timespan)
        {
            if (timespan == null)
            {
                return null;
            }
            return timespan.Value.ToDurationString(
                showHours: false,
                showMinutes: false,
                showSeconds: false
            );
        }
    }
}
