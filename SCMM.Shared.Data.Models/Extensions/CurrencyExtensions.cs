using System;
using System.Globalization;

namespace SCMM.Shared.Data.Models.Extensions
{
    public static class CurrencyExtensions
    {
        public static long CalculateExchange(this IExchangeableCurrency localCurrency, long value, IExchangeableCurrency valueCurrency)
        {
            if (localCurrency == null || valueCurrency == null)
            {
                return 0;
            }

            var localValue = (decimal)value;
            if (valueCurrency != localCurrency)
            {
                var baseValue = value != 0
                    ? value / valueCurrency.ExchangeRateMultiplier
                    : 0m;

                localValue = baseValue * localCurrency.ExchangeRateMultiplier;
            }

            return (long)Math.Floor(localValue);
        }

        public static long CalculateExchange(this IExchangeableCurrency localCurrency, decimal value)
        {
            if (localCurrency == null)
            {
                return 0;
            }

            var localValue = value * localCurrency.ExchangeRateMultiplier;
            return (long)Math.Floor(localValue);
        }

        public static decimal ToPrice<T>(this T currency, long price, IExchangeableCurrency priceCurrency)
            where T : ICurrency, IExchangeableCurrency
        {
            return currency?.ToPrice(currency.CalculateExchange(price, priceCurrency)) ?? 0;
        }

        public static decimal ToPrice(this ICurrency currency, long price)
        {
            if (currency == null)
            {
                return price;
            }

            var localScaleString = string.Empty.PadRight(currency.Scale, '0');
            var localScaleDivisor = long.Parse($"1{localScaleString}");
            var localPrice = Math.Round((decimal)Math.Abs(price) / localScaleDivisor, currency.Scale);
            return localPrice;
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

            var sign = price < 0 ? "-" : string.Empty;
            var localScaleString = string.Empty.PadRight(currency.Scale, '0');
            var localFormat = $"#,##0{(currency.Scale > 0 ? "." : string.Empty)}{localScaleString}";
            var localCulture = CultureInfo.GetCultureInfo(currency.CultureName);
            var localPrice = currency.ToPrice(price);
            var localPriceString = $"{sign}{localPrice.ToString(localFormat, localCulture.NumberFormat)}";
            if (!dense)
            {
                localPriceString = $"{currency.PrefixText}{localPriceString}{currency.SuffixText}";
            }
            return localPriceString.Trim();
        }
    }
}
