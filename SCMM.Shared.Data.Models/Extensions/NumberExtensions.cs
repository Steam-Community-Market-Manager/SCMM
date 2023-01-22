using System;
using System.Net;

namespace SCMM.Shared.Data.Models.Extensions
{
    public static class NumberExtensions
    {
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
    }
}
