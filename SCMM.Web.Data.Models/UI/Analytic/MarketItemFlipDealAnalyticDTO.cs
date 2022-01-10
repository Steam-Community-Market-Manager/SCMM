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

        public string BuyUrl { get; set; }

        public MarketType SellTo { get; set; }

        public long SellNowPrice { get; set; }

        [JsonIgnore]
        public decimal SellNowPriceRatio => BuyPrice > 0 && SellNowPrice > 0 ? (SellNowPrice - BuyPrice) / (decimal)SellNowPrice : 0;

        public long? SellNowFee { get; set; }

        [JsonIgnore]
        public long SellNowProfit => SellNowPrice - (SellNowFee ?? 0) - BuyPrice;

        public long SellLaterPrice { get; set; }

        [JsonIgnore]
        public decimal SellLaterPriceRatio => BuyPrice > 0 && SellLaterPrice > 0 ? (SellLaterPrice - BuyPrice) / (decimal)SellLaterPrice : 0;

        public long? SellLaterFee { get; set; }

        [JsonIgnore]
        public long SellLaterProfit => SellLaterPrice - (SellLaterFee ?? 0) - BuyPrice;
    }
}
