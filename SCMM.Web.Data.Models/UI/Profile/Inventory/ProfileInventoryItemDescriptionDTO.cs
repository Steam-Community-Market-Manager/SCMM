using SCMM.Web.Data.Models.UI.Item;

namespace SCMM.Web.Data.Models.UI.Profile.Inventory
{
    public class ProfileInventoryItemDescriptionDTO : ItemDescriptionWithPriceDTO
    {
        public int Quantity { get; set; }
    }
}
