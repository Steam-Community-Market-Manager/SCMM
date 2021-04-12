using System;

namespace SCMM.Web.Server.Data.Models.Steam
{
    public abstract class SteamMarketItemOrder : Entity
    {
        public long Price { get; set; }

        public int Quantity { get; set; }

        public Guid ItemId { get; set; }

        public SteamMarketItem Item { get; set; }
    }
}
