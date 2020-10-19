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

        // TODO: Replace this with Description.MarketItem
        public Guid? MarketItemId { get; set; }

        // TODO: Replace this with Description.MarketItem
        public SteamMarketItem MarketItem { get; set; }
    }
}
