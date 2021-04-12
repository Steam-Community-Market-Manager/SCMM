using System;

namespace SCMM.Data.Shared.Extensions
{
    public static class NumberExtensions
    {
        public static string ToRegularityString(this long value, long max)
        {
            if (value == 0 || max == 0)
            {
                return null;
            }
            var regularity = Math.Round((decimal)value / max * 100, 2);
            return $"{regularity.ToString("#,##0.00")}%";
        }

        public static string ToQuantityString(this int quantity)
        {
            return quantity > 0
                ? quantity.ToString("#,##")
                : "∞";
        }

        public static string ToQuantityString(this long quantity)
        {
            return quantity > 0
                ? quantity.ToString("#,##")
                : "∞";
        }

        public static string ToMovementString(this long value, long max)
        {
            if (value == 0 || max == 0)
            {
                return null;
            }
            var percentage = (int)Math.Round((decimal)value / max * 100, 0) - 100;
            var prefix = "";
            if (percentage >= 0)
            {
                prefix = "🡱";
            }
            if (percentage < 0)
            {
                prefix = "🡳";
            }
            return $"{prefix} {Math.Abs(percentage).ToString("#,##0")}%".Trim();
        }

        public static string ToPercentageString(this long value, long max)
        {
            if (value == 0 || max == 0)
            {
                return null;
            }
            var percentage = (int)Math.Round((decimal)value / max * 100, 0);
            var prefix = "";
            if (percentage >= 100)
            {
                prefix = "🡱";
            }
            if (percentage < 100)
            {
                prefix = "🡳";
            }
            return $"{prefix} {percentage.ToString("#,##0")}%".Trim();
        }

        public static string ToRoIString(this int percentage)
        {
            var prefix = string.Empty;
            if (percentage >= 0)
            {
                prefix = "🡱";
            }
            if (percentage < 0)
            {
                prefix = "🡳";
            }
            return (percentage != 0 ? $"{prefix} {percentage}%" : "∞").Trim();
        }

        public static string ToRatioPercentageString(this int a, int b)
        {
            if (a == 0 || b == 0)
            {
                return "∞";
            }
            return $"{Math.Abs((int)Math.Floor((double)a / b * 100 - 100)).ToQuantityString()}%";
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
