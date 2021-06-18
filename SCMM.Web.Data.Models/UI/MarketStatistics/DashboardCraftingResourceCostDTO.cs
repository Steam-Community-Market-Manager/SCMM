using SCMM.Web.Data.Models.Domain.Currencies;

namespace SCMM.Web.Data.Models.UI.MarketStatistics
{
    public class DashboardCraftingResourceCostDTO : DashboardAssetDTO
    {
        public CurrencyDTO Currency { get; set; }

        public long BuyNowPrice { get; set; }

        public DashboardAssetMarketValueDTO CheapestItem { get; set; }
    }
}
