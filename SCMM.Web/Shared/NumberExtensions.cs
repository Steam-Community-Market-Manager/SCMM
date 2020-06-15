using SCMM.Web.Shared.Domain.DTOs.Steam;
using System;
using System.Text;

namespace SCMM.Web.Shared
{
    public static class NumberExtensions
    {
        public static string ToPriceString(this SteamCurrencyDTO currency, int price)
        {
            var negative = (price < 0) ? "-" : String.Empty;
            return ($"{currency?.PrefixText} {negative}{Math.Round((decimal)Math.Abs(price) / 100, 2).ToString("#,##0.00")} {currency?.SuffixText}").Trim();
        }

        public static string ToRoIString(this int percentage)
        {
            var prefix = String.Empty;
            if (percentage >= 100)
            {
                prefix = "🡱";
            }

            if (percentage < 100)
            {
                prefix = "🡳";
            }

            return ((percentage > 0) ? $"{prefix} {percentage}%" : "∞").Trim();
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

        public static string ToRegularityString(this int value, int max)
        {
            if (value == 0 || max == 0)
            {
                return null;
            }
            var regularity = Math.Round((((decimal)value / max) * 100), 2);
            return $"{regularity.ToString("#,##0.00")}%";
        }

        public static string ToQuantityString(this int quantity)
        {
            return (quantity > 0)
                ? quantity.ToString("#,##")
                : "∞";
        }

        public static string ToStabilityString(this int value, int max)
        {
            if (value == 0 || max == 0)
            {
                return null;
            }
            var percentage = (int)Math.Round((((decimal)value / max) * 100), 0);
            // Stable
            // Trending Up
            // Trending Down
            return "?";
        }

        public static string ToPercentageString(this int value, int max)
        {
            if (value == 0 || max == 0)
            {
                return null;
            }
            var percentage = (int) Math.Round((((decimal)value / max) * 100), 0);
            var prefix = "";
            if (percentage >= 100)
            {
                prefix = "🡱";
            }
            if (percentage < 100)
            {
                prefix = "🡳";
            }
            return ($"{prefix} {percentage.ToString("#,##0")}%").Trim();
        }

        public static string ToDurationString(this TimeSpan timeSpan, bool showDays = true, bool showHours = true, bool showMinutes = true, bool showSeconds = true)
        {
            if (timeSpan.TotalMinutes <= 0)
            {
                return "just moments ago";
            }

            var text = new StringBuilder();
            if (timeSpan.Days > 0 && showDays == true)
            {
                text.AppendFormat("{0} day{1} ", timeSpan.Days, timeSpan.Days > 1 ? "s" : String.Empty);
            }

            if (timeSpan.Hours > 0 && showHours == true)
            {
                text.AppendFormat("{0} hour{1} ", timeSpan.Hours, timeSpan.Hours > 1 ? "s" : String.Empty);
            }

            if (timeSpan.Minutes > 0 && showMinutes == true)
            {
                text.AppendFormat("{0} minute{1} ", timeSpan.Minutes, timeSpan.Minutes > 1 ? "s" : String.Empty);
            }

            if (timeSpan.Minutes <= 0 && timeSpan.Seconds > 0 && showSeconds == true)
            {
                text.AppendFormat("{0} second{1} ", timeSpan.Seconds, timeSpan.Seconds > 1 ? "s" : String.Empty);
            }

            return text.ToString();
        }

        public static int GCD(int a, int b)
        {
            return (b == 0 ? Math.Abs(a) : GCD(b, a % b));
        }
    }
}
