using SCMM.Steam.Data.Models.Enums;
using System.Text.Json.Serialization;

namespace SCMM.Web.Data.Models.UI.Analytic
{
    public class MarketItemFlipSaleAnalyticDTO
    {
        public ulong Id { get; set; }

        public ulong AppId { get; set; }

        public string IconUrl { get; set; }

        public string Name { get; set; }

        public PriceType BuyFrom { get; set; }

        public long BuyPrice { get; set; }

        public string BuyUrl { get; set; }

        public PriceType SellTo { get; set; }

        public long SellNowPrice { get; set; }

        [JsonIgnore]
        public decimal SellNowPriceRatio => BuyPrice > 0 && SellNowPrice > 0 ? (SellNowPrice - BuyPrice) / (decimal)SellNowPrice : 0;

        public long? SellNowTax { get; set; }

        [JsonIgnore]
        public long SellNowProfit => SellNowPrice - (SellNowTax ?? 0) - BuyPrice;

        public long SellLaterPrice { get; set; }

        [JsonIgnore]
        public decimal SellLaterPriceRatio => BuyPrice > 0 && SellLaterPrice > 0 ? (SellLaterPrice - BuyPrice) / (decimal)SellLaterPrice : 0;

        public long? SellLaterTax { get; set; }

        [JsonIgnore]
        public long SellLaterProfit => SellLaterPrice - (SellLaterTax ?? 0) - BuyPrice;
    }
}
