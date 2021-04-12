using SCMM.Steam.Data.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Steam.Data.Store
{
    public class SteamProfileInventoryItem : SteamItem
    {
        [Required]
        public Guid ProfileId { get; set; }

        public SteamProfile Profile { get; set; }

        public Guid? CurrencyId { get; set; }

        public SteamCurrency Currency { get; set; }

        public SteamProfileInventoryItemAcquisitionType AcquiredBy { get; set; }

        public long? BuyPrice { get; set; }

        public int Quantity { get; set; }

        public SteamProfileInventoryItemFlags Flags { get; set; }
    }
}
