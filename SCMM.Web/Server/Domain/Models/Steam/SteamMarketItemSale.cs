using System;

namespace SCMM.Web.Server.Domain.Models.Steam
{
    public class SteamMarketItemSale : Entity
    {
        public DateTimeOffset Timestamp { get; set; }

        public int Price { get; set; }

        public int Quantity { get; set; }

        public Guid? ItemId { get; set; }

        public SteamMarketItem Item { get; set; }
    }
}
