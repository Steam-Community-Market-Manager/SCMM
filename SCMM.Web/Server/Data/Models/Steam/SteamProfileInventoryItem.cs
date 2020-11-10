using SCMM.Web.Shared.Data.Models.Steam;
using System;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Data.Models.Steam
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
