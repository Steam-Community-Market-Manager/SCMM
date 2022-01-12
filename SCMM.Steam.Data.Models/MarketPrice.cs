using SCMM.Shared.Data.Models;
using SCMM.Steam.Data.Models.Enums;

namespace SCMM.Steam.Data.Models
{
    public class MarketPrice
    {
        public PriceTypes Type { get; set; }

        public MarketType MarketType { get; set; }

        public IExchangeableCurrency Currency { get; set; }

        public long Price { get; set; }

        public long Fee { get; set; }

        /// <summary>
        /// Zero == no supply. Null == unlimited supply. 
        /// </summary>
        public int? Supply { get; set; } = 0;

        public bool IsAvailable { get; set; }

        /// <summary>
        /// If true, price is from a 1st party market, run by Steam/Value
        /// </summary>
        public bool IsFirstPartyMarket => (MarketType == MarketType.SteamStore || MarketType == MarketType.SteamCommunityMarket);

        public string Url { get; set; }
    }
}
