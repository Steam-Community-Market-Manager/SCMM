using SCMM.Web.Data.Models.UI.Item;

namespace SCMM.Web.Data.Models.UI.Statistic
{
    public class ItemResourceCostStatisticDTO : ItemDescriptionDTO
    {
        public long BuyNowPrice { get; set; }

        public ItemValueStatisticDTO CheapestItem { get; set; }
    }
}
