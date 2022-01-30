using SCMM.Steam.Data.Models.Enums;
using System.Text.Json.Serialization;

namespace SCMM.Web.Data.Models.UI.Analytic
{
    public class MarketItemFlipDealAnalyticDTO
    {
        public ulong Id { get; set; }

        public ulong AppId { get; set; }

        public string IconUrl { get; set; }

        public string Name { get; set; }

        public MarketType BuyFrom { get; set; }

        public long BuyPrice { get; set; }

        public long BuyFee { get; set; }

        [JsonIgnore]
        public long BuyTotal => (BuyPrice + BuyFee);

        public string BuyUrl { get; set; }

        public MarketType SellTo { get; set; }

        public long SellLowPrice { get; set; }

        public long SellLowFee { get; set; }

        [JsonIgnore]
        public long SellLowProfit => (SellLowPrice - SellLowFee - BuyTotal);

        public long SellHighPrice { get; set; }

        public long SellHighFee { get; set; }

        [JsonIgnore]
        public long SellHighProfit => (SellHighPrice - SellHighFee - BuyTotal);

    }
}
