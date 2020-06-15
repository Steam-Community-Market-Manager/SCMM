using System;

namespace SCMM.Web.Shared.Domain.DTOs.Steam
{
    public class SteamMarketItemSaleDTO : EntityDTO
    {
        public DateTimeOffset Timestamp { get; set; }

        public int Price { get; set; }

        public int Quantity { get; set; }
    }
}
