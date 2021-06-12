using SCMM.Steam.Data.Models.Enums;
using System;

namespace SCMM.Web.Data.Models.Domain.InventoryItems
{
    public class UpdateInventoryItemCommand
    {
        public SteamProfileInventoryItemAcquisitionType? AcquiredBy { get; set; }

        public Guid? CurrencyId { get; set; }

        public long? BuyPrice { get; set; }
    }
}
