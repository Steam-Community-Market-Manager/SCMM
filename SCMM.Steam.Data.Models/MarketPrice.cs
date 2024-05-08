using SCMM.Shared.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;

namespace SCMM.Steam.Data.Models
{
    public class MarketPrice
    {
        public MarketType MarketType { get; set; }

        public PriceFlags AcceptedPayments { get; set; }

        public IExchangeableCurrency Currency { get; set; }

        /// <summary>
        /// If non-null, this represents the markets in-house currency (e.g. "coins") from which the price is derived
        /// </summary>
        public IExchangeableCurrency HouseCurrency { get; set; }

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
        public bool IsFirstPartyMarket => MarketType.IsFirstParty();

        public string Url { get; set; }
    }
}
