using SCMM.Web.Data.Models.UI.Item;

namespace SCMM.Web.Data.Models.UI.Statistic
{
    public class ItemManipulationStatisticDTO : ItemDescriptionDTO
    {
        public long AverageMarketValue { get; set; }

        public long BuyNowPrice { get; set; }
    }
}
