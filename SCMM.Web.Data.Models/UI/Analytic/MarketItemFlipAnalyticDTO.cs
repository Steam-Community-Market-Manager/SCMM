using SCMM.Steam.Data.Models.Enums;
using System.Text.Json.Serialization;

namespace SCMM.Web.Data.Models.UI.Analytic
{
    public class MarketItemFlipAnalyticDTO
    {
        public ulong Id { get; set; }

        public ulong AppId { get; set; }

        public string IconUrl { get; set; }

        public string Name { get; set; }

        public MarketType BuyFrom { get; set; }

        public DateTimeOffset? BuyPriceLastUpdatedOn { get; set; }

        public int? BuySupplyAvailable { get; set; }

        public PriceFlags? BuyAcceptedPayments { get; set; }

        public long BuyPrice { get; set; }

        public long BuyFee { get; set; }

        [JsonIgnore]
        public long BuyTotal => (BuyPrice + BuyFee);

        public string BuyUrl { get; set; }

        public MarketType SellTo { get; set; }

        public DateTimeOffset? SellPriceLastUpdatedOn { get; set; }

        public long SellPrice { get; set; }

        public long SellFee { get; set; }

        [JsonIgnore]
        public long SellProfit => (SellPrice - SellFee - BuyTotal);

        public bool IsBeingManipulated { get; set; }

        public string ManipulationReason { get; set; }
    }
}
