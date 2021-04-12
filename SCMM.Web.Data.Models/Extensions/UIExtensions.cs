using System;
using System.Collections.Generic;
using System.Linq;

namespace SCMM.Web.Data.Models.Extensions
{
    public static class UIExtensions
    {
        public const int DefaultTolerance = 0;

        public static bool IsStonking(this long now, long longTermAverage, int tolerance = DefaultTolerance)
        {
            return now > longTermAverage && Math.Abs(now - longTermAverage) >= tolerance;
        }

        public static bool IsStinking(this long now, long longTermAverage, int tolerance = DefaultTolerance)
        {
            return now < longTermAverage && Math.Abs(now - longTermAverage) >= tolerance;
        }

        public static bool IsSaturated(this int supply, int demand)
        {
            return supply >= demand;
        }

        public static bool IsStarved(this int supply, int demand)
        {
            return demand > supply;
        }

        public static string ToTextColourClass(this long a, long b, int tolerance = DefaultTolerance)
        {
            var isAboveTolerance = Math.Abs(a - b) >= tolerance;
            if (a > b && isAboveTolerance)
            {
                return "Text-Success";
            }
            else if (a < b && isAboveTolerance)
            {
                return "Text-Error";
            }
            return null;
        }

        public static string ToStabilityString(this long now, long longTermAverage, int tolerance = DefaultTolerance)
        {
            if (now.IsStonking(longTermAverage, tolerance))
            {
                return $"📈 Stonking (+{Math.Abs(now - longTermAverage)})";
            }
            else if (now.IsStinking(longTermAverage, tolerance))
            {
                return $"📉 Stinking (-{Math.Abs(now - longTermAverage)})";
            }
            else
            {
                return "Stable";
            }
        }

        public static IEnumerable<KeyValuePair<string, double>> ToDailyGraphDictionary(this IDictionary<DateTime, double> graph)
        {
            return graph?.ToDictionary(
                x => x.Key.ToString("dd MMM yyyy"),
                x => x.Value
            );
        }

        public static IEnumerable<KeyValuePair<string, double>> ToHourlyGraphDictionary(this IDictionary<DateTime, double> graph)
        {
            return graph?.ToDictionary(
                x => x.Key.ToString("dd MMM yyyy htt"),
                x => x.Value
            );
        }
    }
}
