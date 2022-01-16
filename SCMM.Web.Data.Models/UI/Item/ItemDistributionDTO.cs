using SCMM.Steam.Data.Models.Enums;

namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemDistributionDTO
    {
        public long? EstimatedItemTotalCount { get; set; }

        public long KnownItemTotalCount => (KnownInventoryItemCount + KnownMarketItemCounts.Sum(x => x.Value));

        public int KnownInventoryItemCount { get; set; }

        public IDictionary<MarketType, int> KnownMarketItemCounts { get; set; }
    }
}
