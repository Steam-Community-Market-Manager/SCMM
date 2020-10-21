using System;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Data.Models.Steam
{
    public class SteamInventoryItem : SteamItem
    {
        [Required]
        public Guid OwnerId { get; set; }

        public SteamProfile Owner { get; set; }

        public Guid? CurrencyId { get; set; }

        public SteamCurrency Currency { get; set; }

        public long? BuyPrice { get; set; }

        public int Quantity { get; set; }
    }
}
