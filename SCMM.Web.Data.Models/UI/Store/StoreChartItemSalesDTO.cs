using SCMM.Shared.Data.Models.Extensions;

namespace SCMM.Web.Data.Models.UI.Store
{
    public class StoreChartItemSalesDTO
    {
        public string Name { get; set; }

        public long SupplyTotalEstimated { get; set; }

        public long SupplyTotalMarketsKnown { get; set; }

        public long SupplyTotalInvestorsKnown { get; set; }

        public long SupplyTotalInvestorsEstimated { get; set; }

        public long SupplyTotalOwnersKnown { get; set; }

        public long SupplyTotalOwnersEstimated { get; set; }

        public string TotalText => $"{SupplyTotalEstimated.ToQuantityString()}+ estimated sales";
    }
}
