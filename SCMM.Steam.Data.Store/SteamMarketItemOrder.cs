using SCMM.Data.Shared.Store;
using System;

namespace SCMM.Steam.Data.Store
{
    public abstract class SteamMarketItemOrder : Entity
    {
        public long Price { get; set; }

        public int Quantity { get; set; }

        public Guid ItemId { get; set; }

        public SteamMarketItem Item { get; set; }
    }
}
