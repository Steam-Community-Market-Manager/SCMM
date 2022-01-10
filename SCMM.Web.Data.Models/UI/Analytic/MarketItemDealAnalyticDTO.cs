using SCMM.Steam.Data.Models.Enums;
using System.Text.Json.Serialization;

namespace SCMM.Web.Data.Models.UI.Analytic
{
    public class MarketItemDealAnalyticDTO
    {
        public ulong Id { get; set; }

        public ulong AppId { get; set; }

        public string IconUrl { get; set; }

        public string Name { get; set; }

        public MarketType BuyFrom { get; set; }

        public long BuyPrice { get; set; }

        public string BuyUrl { get; set; }

        public MarketType ReferenceFrom { get; set; }

        public long ReferemcePrice { get; set; }

        [JsonIgnore]
        public long DiscountAmount => Math.Abs(ReferemcePrice - BuyPrice);
    }
}
