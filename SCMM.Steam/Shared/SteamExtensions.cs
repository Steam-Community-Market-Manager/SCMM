using System;

namespace SCMM.Steam.Shared
{
    public static class SteamExtensions
    {
        public static DateTime SteamTimestampToDateTime(this int timestamp)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(timestamp);
        }

        public static string SteamColourToHexString(this string colour)
        {
            // Steam doesn't prefix their colours with a hash
            if (!String.IsNullOrEmpty(colour) && !colour.StartsWith("#"))
                colour = $"#{colour}";
            return colour;
        }
    }
}
