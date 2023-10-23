namespace SCMM.Shared.Data.Models.Extensions
{
    public static class NumberExtensions
    {
        public static bool IsWithinPercentageRangeOf(this long value, long target, decimal targetPercentRange)
        {
            var delta = (target * targetPercentRange);
            return (value <= (target + delta) && value > (target - delta));
        }

        public static string GetPositionSuffix(this int day)
        {
            switch (day)
            {
                case 1:
                case 21:
                case 31:
                    return "st";
                case 2:
                case 22:
                    return "nd";
                case 3:
                case 23:
                    return "rd";
                default:
                    return "th";
            }
        }

        public static string ToFileSizeString(this int size)
        {
            return ((long)size).ToFileSizeString();
        }

        public static string ToFileSizeString(this long size)
        {
            if (size > (1024 * 1024 * 1024))
            {
                return $"{ToQuantityString((long)Math.Round((double)size / (1024 * 1024 * 1024), 0))}GB";
            }
            else if (size > (1024 * 1024))
            {
                return $"{ToQuantityString((long)Math.Round((double)size / (1024 * 1024), 0))}MB";
            }
            else if (size > (1024))
            {
                return $"{ToQuantityString((long)Math.Round((double)size / (1024), 0))}KB";
            }
            else
            {
                return ToQuantityString(size);
            }
        }

        public static string ToQuantityString(this int quantity)
        {
            return ToQuantityString((long)quantity);
        }

        public static string ToQuantityString(this long quantity)
        {
            return quantity > 0
                ? quantity.ToString("#,##")
                : "0";
        }

        public static string ToMovementString(this int value, int max, int decimals = 0)
        {
            return ToMovementString((long)value, (long)max, decimals);
        }

        public static string ToMovementString(this long value, long max, int decimals = 0)
        {
            if (value == 0 || max == 0)
            {
                return null;
            }
            var movement = Math.Abs(100 - Math.Round((decimal)value / max * 100, decimals));
            return $"{movement.ToString($"#,##0{(decimals > 0 ? "." : string.Empty)}{string.Empty.PadRight(decimals, '0')}")}%".Trim();
        }

        public static decimal ToPercentage(this int value, int max, int decimals = 0)
        {
            return ToPercentage((long)value, (long)max, decimals: decimals);
        }

        public static decimal ToPercentage(this long value, long max, int decimals = 0)
        {
            if (value == 0 || max == 0)
            {
                return 0;
            }
            return Math.Round((decimal)value / max * 100, decimals);
        }

        public static string ToPercentageString(this int value, int max, int decimals = 0, bool dense = false)
        {
            return ToPercentageString((long)value, (long)max, decimals: decimals, dense: dense);
        }

        public static string ToPercentageString(this long value, long max, int decimals = 0, bool dense = false)
        {
            if (value == 0 || max == 0)
            {
                return null;
            }
            var percentage = value.ToPercentage(max, decimals);
            var percentageString = percentage.ToString($"#,##0{(decimals > 0 ? "." : string.Empty)}{string.Empty.PadRight(decimals, '0')}");
            return ($"{percentageString}{(!dense ? "%" : null)}").Trim();
        }

        public static string ToRoIString(this int percentage)
        {
            return (percentage != 0 ? $"{percentage}%" : "-").Trim();
        }

        public static string ToGCDRatioString(this int a, int b)
        {
            if (a == 0 || b == 0)
            {
                return "∞";
            }
            var gcd = GCD(a, b);
            return $"{a / gcd}:{b / gcd}";
        }

        public static long GCD(long a, long b)
        {
            return b == 0 ? Math.Abs(a) : GCD(b, a % b);
        }

        public static IEnumerable<decimal> CumulativeMovingAverage(this IEnumerable<decimal> source)
        {
            ulong count = 0;
            decimal sum = 0;

            foreach (var d in source)
            {
                yield return (sum += d) / ++count;
            }
        }

        public static IEnumerable<decimal> SimpleMovingAverage(this IEnumerable<decimal> source, int length)
        {
            var sample = new Queue<decimal>(length);

            foreach (var d in source)
            {
                if (sample.Count == length)
                {
                    sample.Dequeue();
                }
                sample.Enqueue(d);
                yield return sample.Average();
            }
        }

        public static IEnumerable<decimal> ExponentialMovingAverage(this IEnumerable<decimal> source, int length)
        {
            var alpha = 2 / (decimal)(length + 1);
            var s = source.ToArray();
            decimal result = 0;

            for (var i = 0; i < s.Length; i++)
            {
                result = i == 0
                    ? s[i]
                    : alpha * s[i] + (1 - alpha) * result;
                yield return result;
            }
        }

        public static decimal Delta(this IEnumerable<decimal> source)
        {
            return source.LastOrDefault() - source.FirstOrDefault();
        }

        public static int TotalIncrementCount(this decimal[] source)
        {
            var numberOfIncrements = 0;
            for(int i = 1; i < source.Length - 1; i++)
            {
                if (source[i] > source[i - 1])
                {
                    numberOfIncrements++;
                }
            }   

            return numberOfIncrements;
        }
    }
}
