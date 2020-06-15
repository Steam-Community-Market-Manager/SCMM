using System;

namespace SCMM.Web.Server.Domain.Models.Steam
{
    public class SteamMarketItemOrder : Entity
    {
        public int Price { get; set; }

        public int Quantity { get; set; }

        public Guid? BuyItemId { get; set; }

        public SteamMarketItem BuyItem { get; set; }

        public Guid? SellItemId { get; set; }

        public SteamMarketItem SellItem { get; set; }

    }
}
