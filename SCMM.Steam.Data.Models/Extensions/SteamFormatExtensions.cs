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
    }
}
