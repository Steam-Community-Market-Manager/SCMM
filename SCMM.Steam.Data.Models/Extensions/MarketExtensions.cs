using SCMM.Steam.Data.Models.Attributes;
using SCMM.Steam.Data.Models.Enums;
using System.Reflection;

namespace SCMM.Steam.Data.Models.Extensions
{
    public static class MarketExtensions
    {
        public static bool IsAppSupported(this MarketType marketType, ulong appId)
        {
            return GetSupportedAppIds(marketType)?.Contains(appId) == true;
        }

        public static ulong[] GetSupportedAppIds(this MarketType marketType)
        {
            var marketTypeField = typeof(MarketType).GetField(marketType.ToString(), BindingFlags.Public | BindingFlags.Static);
            var market = marketTypeField?.GetCustomAttribute<MarketAttribute>();
            return market?.SupportedAppIds ?? new ulong[0];
        }

        public static bool IsFirstParty(this MarketType marketType)
        {
            var marketTypeField = typeof(MarketType).GetField(marketType.ToString(), BindingFlags.Public | BindingFlags.Static);
            var market = marketTypeField?.GetCustomAttribute<MarketAttribute>();
            return market?.IsFirstParty ?? false;
        }

        public static string GetColor(this MarketType marketType)
        {
            var marketTypeField = typeof(MarketType).GetField(marketType.ToString(), BindingFlags.Public | BindingFlags.Static);
            var market = marketTypeField?.GetCustomAttribute<MarketAttribute>();
            return market?.Color;
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
    }
}
