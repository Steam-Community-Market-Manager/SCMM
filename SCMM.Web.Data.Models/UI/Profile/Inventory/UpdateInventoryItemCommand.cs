using SCMM.Steam.Data.Models.Enums;

namespace SCMM.Web.Data.Models.UI.Profile.Inventory
{
    public class UpdateInventoryItemCommand
    {
        public SteamProfileInventoryItemAcquisitionType? AcquiredBy { get; set; }

        public Guid? CurrencyGuid { get; set; }

        public long? BuyPrice { get; set; }
    }
}
