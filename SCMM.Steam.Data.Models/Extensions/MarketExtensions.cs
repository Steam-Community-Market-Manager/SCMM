
using SCMM.Steam.Data.Models.Attributes;
using SCMM.Steam.Data.Models.Enums;
using System.Reflection;

namespace SCMM.Steam.Data.Models.Extensions
{
    public static class MarketExtensions
    {
        public static bool IsAppSupported(this MarketType marketType, ulong appId)
        {
            return GetMarketAppIds(marketType)?.Contains(appId) == true;
        }

        public static ulong[] GetMarketAppIds(this MarketType marketType)
        {
            var marketTypeField = typeof(MarketType).GetField(marketType.ToString(), BindingFlags.Public | BindingFlags.Static);
            var market = marketTypeField?.GetCustomAttribute<MarketAttribute>();
            return market?.Apps ?? new ulong[0];
        }

        public static PriceTypes? GetMarketPriceType(this MarketType marketType)
        {
            var marketTypeField = typeof(MarketType).GetField(marketType.ToString(), BindingFlags.Public | BindingFlags.Static);
            var market = marketTypeField?.GetCustomAttribute<MarketAttribute>();
            return market?.Type;
        }

        public static long GetMarketBuyFees(this MarketType marketType, long price)
        {
            var fee = 0L;
            var marketTypeField = typeof(MarketType).GetField(marketType.ToString(), BindingFlags.Public | BindingFlags.Static);
            var marketBuyFrom = marketTypeField?.GetCustomAttribute<BuyFromAttribute>();
            if (marketBuyFrom.FeeRate != 0 && price > 0)
            {
                fee += price.MarketSaleFeeComponentAsInt(marketBuyFrom.FeeRate);
            }
            if (marketBuyFrom.FeeSurcharge != 0 && price > 0)
            {
                fee += marketBuyFrom.FeeSurcharge;
            }

            return fee;
        }

        public static string GetMarketBuyUrl(this MarketType marketType, string appId, string appName, ulong? classId, string name)
        {
            var marketTypeField = typeof(MarketType).GetField(marketType.ToString(), BindingFlags.Public | BindingFlags.Static);
            var marketBuyFrom = marketTypeField?.GetCustomAttribute<BuyFromAttribute>();
            return String.Format(
                (marketBuyFrom?.Url ?? String.Empty),
                Uri.EscapeDataString(appId ?? String.Empty), Uri.EscapeDataString(appName?.ToLower() ?? String.Empty), classId, Uri.EscapeDataString(name ?? String.Empty)
            );
        }
    }
}
