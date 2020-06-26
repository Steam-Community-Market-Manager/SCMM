using System;

namespace SCMM.Web.Server.Domain.Models.Steam
{
    public abstract class SteamMarketItemOrder : Entity
    {
        public int Price { get; set; }

        public int Quantity { get; set; }

        public Guid ItemId { get; set; }

        public SteamMarketItem Item { get; set; }
    }
}
