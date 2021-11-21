using System.Text;

namespace SCMM.Shared.Data.Models.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTimeOffset? Earliest(this DateTimeOffset? a, DateTimeOffset? b)
        {
            return (a != null && b != null && a <= b) ? (a ?? b) : (b ?? a);
        }

        public static DateTimeOffset? Latest(this DateTimeOffset? a, DateTimeOffset? b)
        {
            return (a != null && b != null && a >= b) ? (a ?? b) : (b ?? a);
        }

        public static string GetDaySuffix(this DateTimeOffset dateTime)
        {
            return dateTime.Day.GetPositionSuffix();
        }

        public static string ToDurationString(this TimeSpan timeSpan, bool showYears = true, bool showMonths = true, bool showWeeks = true,
            bool showDays = true, bool showHours = true, bool showMinutes = true, bool showSeconds = true, string prefix = null, string suffix = null, string zero = null, int maxGranularity = 7)
        {
            if (timeSpan <= TimeSpan.Zero)
            {
                return zero;
            }

            var text = new StringBuilder();
            var granularity = maxGranularity;
            var days = timeSpan.Days;
            var years = (int)Math.Floor(days > 0 ? (decimal)days / 365 : 0);
            if (years > 0 && showYears && granularity > 0)
            {
                if (text.Length > 0)
                {
                    text.Append(", ");
                }

                text.AppendFormat("{0} year{1}", years.ToString("#,##"), years > 1 ? "s" : string.Empty);
                days -= years * 365;
                granularity--;
            }
            var months = (int)Math.Floor(days > 0 ? (decimal)days / 30 : 0);
            if (months > 0 && showMonths && granularity > 0)
            {
                if (text.Length > 0)
                {
                    text.Append(", ");
                }

                text.AppendFormat("{0} month{1}", months.ToString("#,##"), months > 1 ? "s" : string.Empty);
                days -= months * 30;
                granularity--;
            }
            var weeks = (int)Math.Floor(days > 0 ? (decimal)days / 7 : 0);
            if (weeks > 0 && showWeeks && granularity > 0)
            {
                if (text.Length > 0)
                {
                    text.Append(", ");
                }

                text.AppendFormat("{0} week{1}", weeks.ToString("#,##"), weeks > 1 ? "s" : string.Empty);
                days -= weeks * 7;
                granularity--;
            }
            if (days > 0 && showDays && granularity > 0)
            {
                if (text.Length > 0)
                {
                    text.Append(", ");
                }

                text.AppendFormat("{0} day{1}", days.ToString("#,##"), days > 1 ? "s" : string.Empty);
                granularity--;
            }

            if (timeSpan.Hours > 0 && showHours && granularity > 0)
            {
                if (text.Length > 0)
                {
                    text.Append(", ");
                }

                text.AppendFormat("{0} hour{1}", timeSpan.Hours, timeSpan.Hours > 1 ? "s" : string.Empty);
                granularity--;
            }
            if (timeSpan.Minutes > 0 && showMinutes && granularity > 0)
            {
                if (text.Length > 0)
                {
                    text.Append(", ");
                }

                text.AppendFormat("{0} minute{1}", timeSpan.Minutes, timeSpan.Minutes > 1 ? "s" : string.Empty);
                granularity--;
            }
            if (timeSpan.Minutes <= 0 && timeSpan.Seconds > 0 && showSeconds && granularity > 0)
            {
                if (text.Length > 0)
                {
                    text.Append(", ");
                }

                text.AppendFormat("{0} second{1}", timeSpan.Seconds, timeSpan.Seconds > 1 ? "s" : string.Empty);
                granularity--;
            }

            if (text.Length != 0)
            {
                if (!string.IsNullOrEmpty(prefix))
                {
                    text.Insert(0, $"{prefix} ");
                }
                if (!string.IsNullOrEmpty(suffix))
                {
                    text.Append($" {suffix}");
                }
            }

            return text.Length > 0
                ? text.ToString()
                : zero;
        }
    }
}
