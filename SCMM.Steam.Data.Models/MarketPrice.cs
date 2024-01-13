using SCMM.Shared.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;

namespace SCMM.Steam.Data.Models
{
    public class MarketPrice
    {
        public MarketType MarketType { get; set; }

        public PriceTypes AcceptedPaymentTypes { get; set; }

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
        public bool IsFirstPartyMarket => MarketType.IsFirstParty();

        public string Url { get; set; }
    }
}
