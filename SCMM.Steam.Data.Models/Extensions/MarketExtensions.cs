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

        public static IEnumerable<BuyFromAttribute> GetBuyFromOptions(this MarketType marketType)
        {
            var marketTypeField = typeof(MarketType).GetField(marketType.ToString(), BindingFlags.Public | BindingFlags.Static);
            return marketTypeField?.GetCustomAttributes<BuyFromAttribute>() ?? Enumerable.Empty<BuyFromAttribute>();
        }

        public static IEnumerable<SellToAttribute> GetSellToOptions(this MarketType marketType)
        {
            var marketTypeField = typeof(MarketType).GetField(marketType.ToString(), BindingFlags.Public | BindingFlags.Static);
            return marketTypeField?.GetCustomAttributes<SellToAttribute>() ?? Enumerable.Empty<SellToAttribute>();
        }

        private static T GetCustomAttribute<T>(this MarketType marketType) where T : Attribute
        {
            var marketTypeField = typeof(MarketType).GetField(marketType.ToString(), BindingFlags.Public | BindingFlags.Static);
            return marketTypeField?.GetCustomAttribute<T>();
        }
    }
}
