using SCMM.Web.Data.Models.UI.Item;

namespace SCMM.Web.Data.Models.UI.Statistic
{
    public class ItemBuySellOrderStatisticDTO : ItemDescriptionDTO
    {
        public long BuyNowPrice { get; set; }

        public long BuyAskingPrice { get; set; }
    }
}
