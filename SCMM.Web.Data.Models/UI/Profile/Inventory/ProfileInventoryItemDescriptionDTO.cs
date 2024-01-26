using SCMM.Web.Data.Models.UI.Item;

namespace SCMM.Web.Data.Models.UI.Profile.Inventory
{
    public class ProfileInventoryItemDescriptionDTO : ItemDescriptionWithPriceDTO
    {
        public int Quantity { get; set; }

        public ProfileInventoryItemDescriptionStackDTO[] Stacks { get; set; }

        public long? AverageBuyPrice { get; set; }

        public long? TotalBuyNowPrice => (BuyNowPrice * Quantity) ?? 0;
    }
}
