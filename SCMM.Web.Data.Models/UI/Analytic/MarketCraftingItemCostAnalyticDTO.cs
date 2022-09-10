using SCMM.Steam.Data.Models.Enums;
using SCMM.Web.Data.Models.UI.Item;
using SCMM.Web.Data.Models.UI.Statistic;
using System.Text.Json.Serialization;

namespace SCMM.Web.Data.Models.UI.Analytic
{
    public class MarketCraftingItemCostAnalyticDTO : ItemDescriptionDTO
    {
        public MarketType BuyFrom { get; set; }

        public long BuyPrice { get; set; }

        public long BuyFee { get; set; }

        [JsonIgnore]
        public long BuyTotal => (BuyPrice + BuyFee);

        public string BuyUrl { get; set; }

        public ItemValueStatisticDTO CheapestItem { get; set; }
    }
}
