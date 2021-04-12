using System;

namespace SCMM.Steam.Data.Store.Models.Steam
{
    public class SteamMarketItemSale : Entity
    {
        public DateTimeOffset Timestamp { get; set; }

        public long Price { get; set; }

        public int Quantity { get; set; }

        public Guid ItemId { get; set; }

        public SteamMarketItem Item { get; set; }
    }
}
