using SCMM.Steam.Data.Models.Attributes;
using SCMM.Steam.Data.Models.Enums;
using System.Reflection;

namespace SCMM.Steam.Data.Models.Extensions
{
    public static class MarketExtensions
    {
        public static string GetColor(this MarketType marketType)
        {
            return GetCustomAttribute<MarketAttribute>(marketType)?.Color;
        }

        public static string GetAffiliateUrl(this MarketType marketType)
        {
            return GetCustomAttribute<MarketAttribute>(marketType)?.AffiliateUrl;
        }

        public static bool IsFirstParty(this MarketType marketType)
        {
            return GetCustomAttribute<MarketAttribute>(marketType)?.IsFirstParty ?? false;
        }

        public static bool IsCasino(this MarketType marketType)
        {
            return GetCustomAttribute<MarketAttribute>(marketType)?.IsCasino ?? false;
        }

        public static bool IsAppSupported(this MarketType marketType, ulong appId)
        {
            return GetSupportedAppIds(marketType)?.Contains(appId) == true;
        }

        public static ulong[] GetSupportedAppIds(this MarketType marketType)
        {
            return GetCustomAttribute<MarketAttribute>(marketType)?.SupportedAppIds ?? new ulong[0];
        }

        public static IEnumerable<BuyFromAttribute> GetBuyFromOptions(this MarketType marketType, PriceFlags? withAcceptedPayments = null)
        {
            var marketTypeField = typeof(MarketType).GetField(marketType.ToString(), BindingFlags.Public | BindingFlags.Static);
            return (marketTypeField?.GetCustomAttributes<BuyFromAttribute>() ?? Enumerable.Empty<BuyFromAttribute>())
                ?.Where(x => withAcceptedPayments == null || ((int)x.AcceptedPayments & (int)withAcceptedPayments) != 0);
        }

        public static BuyFromAttribute GetCheapestBuyOption(this MarketType marketType, PriceFlags? withAcceptedPayments = null, bool includeFees = true)
        {
            return GetBuyFromOptions(marketType, withAcceptedPayments)
                ?.OrderBy(x => x.CalculateBuyPrice(100) + (includeFees ? x.CalculateBuyFees(100) : 0))
                ?.FirstOrDefault();
        }

        public static IEnumerable<SellToAttribute> GetSellToOptions(this MarketType marketType, PriceFlags? withAcceptedPayments = null)
        {
            var marketTypeField = typeof(MarketType).GetField(marketType.ToString(), BindingFlags.Public | BindingFlags.Static);
            return (marketTypeField?.GetCustomAttributes<SellToAttribute>() ?? Enumerable.Empty<SellToAttribute>())
                ?.Where(x => withAcceptedPayments == null || ((int)x.AcceptedPayments & (int)withAcceptedPayments) != 0);
        }

        public static SellToAttribute GetPriciestSellOption(this MarketType marketType, PriceFlags? withAcceptedPayments = null, bool includeFees = true)
        {
            return GetSellToOptions(marketType, withAcceptedPayments)
                ?.Where(x => withAcceptedPayments == null || ((int)x.AcceptedPayments & (int)withAcceptedPayments) != 0)
                ?.OrderBy(x => x.CalculateSellPrice(100) + (includeFees ? x.CalculateSellFees(100) : 0))
                ?.FirstOrDefault();
        }

        private static T GetCustomAttribute<T>(this MarketType marketType) where T : Attribute
        {
            var marketTypeField = typeof(MarketType).GetField(marketType.ToString(), BindingFlags.Public | BindingFlags.Static);
            return marketTypeField?.GetCustomAttribute<T>();
        }
    }
}
