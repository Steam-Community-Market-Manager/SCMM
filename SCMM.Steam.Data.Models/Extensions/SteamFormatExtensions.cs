using System.Globalization;

namespace SCMM.Steam.Data.Models.Extensions
{
    public static class SteamFormatExtensions
    {
        public static DateTimeOffset SteamTimestampToDateTimeOffset(this ulong timestamp)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return new DateTimeOffset(epoch.AddSeconds(timestamp), TimeZoneInfo.Utc.BaseUtcOffset);
        }

        public static DateTimeOffset SteamTimestampToDateTimeOffset(this string timestamp)
        {
            DateTimeOffset result;
            if (DateTimeOffset.TryParseExact(timestamp, "yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out result))
            {
                return result;
            }

            return default;
        }

        public static string SteamColourToWebHexString(this string colour)
        {
            // Steam doesn't prefix their colours with a hash
            if (!string.IsNullOrEmpty(colour) && !colour.StartsWith("#"))
            {
                colour = $"#{colour}";
            }

            return colour;
        }

        /// <see cref="https://partner.steamgames.com/doc/features/inventory/schema#SpecifyPrices" />
        public static IEnumerable<SteamPriceInfo> ParseSteamPrices(this string priceText)
        {
            if (!string.IsNullOrEmpty(priceText))
            {
                var priceParts = priceText.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                var priceFormatVersion = priceParts[0];
                if (priceFormatVersion != "1" || priceParts.Length < 2)
                {
                    throw new ArgumentException($"Unsupported price category version '{priceFormatVersion}' ({priceParts.Length})");
                }

                foreach (var priceListPart in priceParts.Skip(1))
                {
                    var prices = priceListPart;
                    var startDate = default(DateTime?);
                    var endDate = default(DateTime?);
                    if (prices.Contains('-'))
                    {
                        var dateParts = priceListPart.Split('-', StringSplitOptions.TrimEntries);
                        startDate = DateTime.ParseExact(dateParts[0].Substring(0, 16), "yyyyMMddTHHmmssZ", null);
                        endDate = DateTime.ParseExact(dateParts[1].Substring(0, 16), "yyyyMMddTHHmmssZ", null);
                        prices = prices.Substring(33);
                    }
                    foreach (var price in prices.Split(','))
                    {
                        var currency = price.Substring(0, 3);
                        var amount = ulong.Parse(price.Substring(3));

                        // Convert VLV price categories int to USD (e.g. VLV1500 = USD1499)
                        if (currency == Constants.SteamCurrencyVLV)
                        {
                            currency = Constants.SteamCurrencyUSD;
                            amount -= 1;
                        }

                        yield return new SteamPriceInfo
                        {
                            Currency = currency,
                            Price = amount
                        };
                    }
                }
            }
        }

        public static SteamPriceInfo SteamPriceAtDateTime(this IEnumerable<SteamPriceInfo> prices, DateTime dateTime, string currency = null)
        {
            return prices
                ?.Where(x => String.IsNullOrEmpty(currency) || x.Currency == currency)
                ?.FirstOrDefault(p => (p.StartDate == null || dateTime >= p.StartDate) && (p.EndDate == null || dateTime < p.EndDate));
        }
    }
}
