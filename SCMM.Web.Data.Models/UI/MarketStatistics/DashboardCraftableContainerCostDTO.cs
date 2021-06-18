using SCMM.Web.Data.Models.Domain.Currencies;
using System.Collections.Generic;
using System.Linq;

namespace SCMM.Web.Data.Models.UI.MarketStatistics
{
    public class DashboardCraftableContainerCostDTO : DashboardAssetDTO
    {
        public CurrencyDTO Currency { get; set; }

        public long BuyNowPrice { get; set; }

        public long CraftingCost => CraftingComponents.Sum(x => x.Component.BuyNowPrice * x.Quantity);

        public IEnumerable<DashboardCraftableContainerComponentCostDTO> CraftingComponents { get; set; }
    }

    public class DashboardCraftableContainerComponentCostDTO
    {
        public DashboardAssetMarketValueDTO Component { get; set; }

        public uint Quantity { get; set; }
    }
}
