using System;
using System.Globalization;
using System.Text;

namespace SCMM.Web.Shared
{
    public static class NumberExtensions
    {
        public const int DefaultTolerance = 0;

        public static bool IsStonking(this long now, long longTermAverage, int tolerance = DefaultTolerance)
        {
            return (now > longTermAverage && (Math.Abs(now - longTermAverage) >= tolerance));
        }

        public static bool IsStinking(this long now, long longTermAverage, int tolerance = DefaultTolerance)
        {
            return (now < longTermAverage && (Math.Abs(now - longTermAverage) >= tolerance));
        }

        public static string ToTextColourClass(this long a, long b, int tolerance = DefaultTolerance)
        {
            var isAboveTolerance = (Math.Abs(a - b) >= tolerance);
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

        public static long CalculateExchange(this IExchangeableCurrency localCurrency, long value, IExchangeableCurrency valueCurrency)
        {
            if (localCurrency == null || valueCurrency == null)
            {
                return 0;
            }

            var localValue = (decimal)value;
            if (valueCurrency != localCurrency)
            {
                var baseValue = (value > 0)
                    ? (value / valueCurrency.ExchangeRateMultiplier)
                    : 0m;

                localValue = (baseValue * localCurrency.ExchangeRateMultiplier);
            }

            return (long)Math.Floor(localValue);
        }

        public static long CalculateExchange(this IExchangeableCurrency localCurrency, decimal value)
        {
            if (localCurrency == null)
            {
                return 0;
            }

            var localValue = (value * localCurrency.ExchangeRateMultiplier);
            return (long)Math.Floor(localValue);
        }

        public static string ToPriceString<T>(this T currency, long price, IExchangeableCurrency priceCurrency, bool dense = false)
            where T : ICurrency, IExchangeableCurrency
        {
            return currency?.ToPriceString(currency.CalculateExchange(price, priceCurrency), dense: dense);
        }

        public static string ToPriceString(this ICurrency currency, long price, bool dense = false)
        {
            if (currency == null)
            {
                return price.ToString();
            }

            var sign = (price < 0) ? "-" : String.Empty;
            var localScaleString = String.Empty.PadRight(currency.Scale, '0');
            var localScaleDivisor = Int64.Parse($"1{localScaleString}");
            var localFormat = $"#,##0{(currency.Scale > 0 ? "." : String.Empty)}{localScaleString}";
            var localCulture = CultureInfo.GetCultureInfo(currency.CultureName);
            var localPrice = Math.Round((decimal)Math.Abs(price) / localScaleDivisor, currency.Scale);
            var localPriceString = $"{sign}{localPrice.ToString(localFormat, localCulture.NumberFormat)}";
            if (!dense)
            {
                localPriceString = $"{currency.PrefixText}{localPriceString}{currency.SuffixText}";
            }
            return localPriceString.Trim();
        }

        public static string ToRegularityString(this long value, long max)
        {
            if (value == 0 || max == 0)
            {
                return null;
            }
            var regularity = Math.Round((((decimal)value / max) * 100), 2);
            return $"{regularity.ToString("#,##0.00")}%";
        }

        public static string ToSalesActivityString(this long sales)
        {
            var suffix = "";
            switch (sales)
            {
                case var _ when (sales >= 3000): suffix = "🔥🔥🔥🔥🔥"; break;
                case var _ when (sales >= 1500): suffix = "🔥🔥🔥🔥"; break;
                case var _ when (sales >= 1000): suffix = "🔥🔥🔥"; break;
                case var _ when (sales >= 500): suffix = "🔥🔥"; break;
                case var _ when (sales >= 250): suffix = "🔥"; break;
                default: break;
            }
            return ($"{sales} {suffix}").Trim();
        }

        public static string ToQuantityString(this int quantity)
        {
            return (quantity > 0)
                ? quantity.ToString("#,##")
                : "∞";
        }

        public static string ToStabilityString(this long now, long longTermAverage, int tolerance = DefaultTolerance)
        {
            if (IsStonking(now, longTermAverage, tolerance))
            {
                return $"📈 Stonking (+{Math.Abs(now - longTermAverage)})";
            }
            else if (IsStinking(now, longTermAverage, tolerance))
            {
                return $"📉 Stinking (-{Math.Abs(now - longTermAverage)})";
            }
            else
            {
                return "Stable";
            }
        }

        public static string ToMovementString(this long now, long longTermAverage, int tolerance = DefaultTolerance)
        {
            if (IsStonking(now, longTermAverage, tolerance))
            {
                return $"🡱 +{Math.Abs(now - longTermAverage)}";
            }
            else if (IsStinking(now, longTermAverage, tolerance))
            {
                return $"🡳 -{Math.Abs(now - longTermAverage)}";
            }
            else
            {
                return "Stable";
            }
        }

        public static string ToPercentageString(this long value, long max)
        {
            if (value == 0 || max == 0)
            {
                return null;
            }
            var percentage = (int)Math.Round((((decimal)value / max) * 100), 0);
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

        public static string ToRoIString(this int percentage)
        {
            var prefix = String.Empty;
            if (percentage >= 0)
            {
                prefix = "🡱";
            }
            if (percentage < 0)
            {
                prefix = "🡳";
            }
            return ((percentage != 0) ? $"{prefix} {percentage}%" : "∞").Trim();
        }

        public static string ToGCDRatioString(this long a, long b)
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
            return (b == 0 ? Math.Abs(a) : GCD(b, a % b));
        }

        public static string ToDurationString(this TimeSpan timeSpan, bool showDays = true, bool showHours = true, bool showMinutes = true, bool showSeconds = true, string suffix = null, string zero = null)
        {
            if (timeSpan <= TimeSpan.Zero)
            {
                return zero;
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
            if (text.Length != 0 && !String.IsNullOrEmpty(suffix))
            {
                text.Append($" {suffix}");
            }

            return (text.Length > 0)
                ? text.ToString()
                : zero;
        }
    }
}
