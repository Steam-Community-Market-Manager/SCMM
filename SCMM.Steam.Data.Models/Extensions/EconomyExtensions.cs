using System;
using System.Globalization;
using System.Linq;

namespace SCMM.Steam.Data.Models
{
    public static class EconomyExtensions
    {
        public const decimal FeeMultiplier = 0.1304347826071739m; // 13%
        public const decimal FeePlatformMultiplier = 0.0304347811170578m; // 3%
        public const decimal FeePublisherMultiplier = 0.100000001490116119m; // 10%

        public static long SteamFeePlatformComponentAsInt(this long value)
        {
            // Minimum platform fee is 0.01 units
            return (long)Math.Floor(Math.Max(value * FeePlatformMultiplier, 1));
        }

        public static long SteamFeePublisherComponentAsInt(this long value)
        {
            // Minimum publisher fee is 0.01 units
            return (long)Math.Floor(Math.Max(value * FeePublisherMultiplier, 1));
        }

        public static long SteamFeeAsInt(this long value)
        {
            // Add both fees together, minimum combined fee is 0.02 units
            return (SteamFeePlatformComponentAsInt(value) + SteamFeePublisherComponentAsInt(value));
        }

        /// <summary>
        /// C# port of the Steam economy common logic
        /// https://steamcommunity-a.akamaihd.net/public/javascript/economy_common.js?v=tsXdRVB0yEaR&l=english
        /// </summary>
        public static int SteamQuantityValueAsInt(this string strAmount, CultureInfo culture = null)
        {
            if (String.IsNullOrEmpty(strAmount))
            {
                return 0;
            }

            strAmount = new string(strAmount.Where(c => char.IsDigit(c)).ToArray());
            return int.Parse(strAmount, culture?.NumberFormat);
        }

        /// <summary>
        /// C# port of the Steam economy common logic
        /// https://steamcommunity-a.akamaihd.net/public/javascript/economy_common.js?v=tsXdRVB0yEaR&l=english
        /// </summary>
        public static long SteamPriceAsInt(this string strAmount, CultureInfo culture = null, bool useDecimalShortCircuit = true)
        {
            long nAmount = 0;
            if (String.IsNullOrEmpty(strAmount))
            {
                return 0;
            }

            // Custom work around for strings that have more than 2 decimal places (e.g. "12.34.56"), round down
            if (useDecimalShortCircuit)
            {
                var decAmount = 0m;
                if (decimal.TryParse(strAmount, NumberStyles.Any, culture?.NumberFormat, out decAmount))
                {
                    decAmount = Math.Round(decAmount, 2);
                    strAmount = decAmount.ToString();
                }
            }

            // Users may enter either comma or period for the decimal mark and digit group separators.
            strAmount = strAmount.Replace(',', '.');

            // strip the currency symbol, set .-- to .00
            strAmount = strAmount.Replace(".--", ".00");
            strAmount = new string(strAmount.Where(c => char.IsDigit(c) || c == '.').ToArray()).Trim('.');

            // strip spaces
            strAmount = strAmount.Replace(" ", String.Empty);

            // Remove all but the last period so that entries like "1,147.6" work
            if (strAmount.IndexOf('.') != -1)
            {
                var splitAmount = strAmount.Split('.');
                var strLastSegment = splitAmount.Length > 0 ? splitAmount[splitAmount.Length - 1] : null;

                if (!String.IsNullOrEmpty(strLastSegment) && strLastSegment.Length == 3 && Int64.Parse(splitAmount[splitAmount.Length - 2], culture?.NumberFormat) != 0)
                {
                    // Looks like the user only entered thousands separators. Remove all commas and periods.
                    // Ensures an entry like "1,147" is not treated as "1.147"
                    //
                    // Users may be surprised to find that "1.147" is treated as "1,147". "1.147" is either an error or the user
                    // really did mean one thousand one hundred and forty seven since no currencies can be split into more than
                    // hundredths. If it was an error, the user should notice in the next step of the dialog and can go back and
                    // correct it. If they happen to not notice, it is better that we list the item at a higher price than
                    // intended instead of lower than intended (which we would have done if we accepted the 1.147 value as is).
                    strAmount = String.Join(String.Empty, splitAmount);
                }
                else
                {
                    strAmount = String.Join(String.Empty, splitAmount.Take(splitAmount.Length - 1)) + '.' + strLastSegment;
                }
            }

            var flAmount = decimal.Parse(strAmount, culture?.NumberFormat) * 100;
            nAmount = (long)Math.Floor(flAmount + 0.000001m); // round down

            nAmount = Math.Max(nAmount, 0);
            return nAmount;
        }
    }
}
