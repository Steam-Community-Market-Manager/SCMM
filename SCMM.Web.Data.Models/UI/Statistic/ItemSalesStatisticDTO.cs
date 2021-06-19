using SCMM.Web.Data.Models.UI.Item;

namespace SCMM.Web.Data.Models.UI.Statistic
{
    public class ItemSalesStatisticDTO : ItemDescriptionDTO
    {
        public long? Subscriptions { get; set; }

        public long? KnownInventoryDuplicates { get; set; }

        public long? EstimatedOtherDuplicates { get; set; }

        public long Total => ((Subscriptions ?? 0) + (KnownInventoryDuplicates ?? 0) + (EstimatedOtherDuplicates ?? 0));
    }
}
