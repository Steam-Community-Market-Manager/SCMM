using SCMM.Steam.Data.Models.Enums;
using SCMM.Web.Data.Models.UI.Item;
using SCMM.Web.Data.Models.UI.Statistic;
using System.Text.Json.Serialization;

namespace SCMM.Web.Data.Models.UI.Analytic
{
    public class MarketCraftableItemCostAnalyticDTO : ItemDescriptionDTO
    {
        public MarketType BuyFrom { get; set; }

        public long BuyPrice { get; set; }

        public long BuyFee { get; set; }

        [JsonIgnore]
        public long BuyTotal => (BuyPrice + BuyFee);

        public string BuyUrl { get; set; }

        [JsonIgnore]
        public long CraftingPrice => CraftingComponents.Sum(x => x.Component.BuyNowPrice * x.Quantity);

        public IEnumerable<ItemCraftingComponentCostDTO> CraftingComponents { get; set; }
    }

    public class ItemCraftingComponentCostDTO
    {
        public string Name { get; set; }

        public uint Quantity { get; set; }

        public ItemValueStatisticDTO Component { get; set; }
    }
}
