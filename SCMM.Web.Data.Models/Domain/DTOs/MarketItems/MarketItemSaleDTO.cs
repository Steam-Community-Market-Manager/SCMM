using System;

namespace SCMM.Web.Data.Models.Domain.DTOs.MarketItems
{
    public class MarketItemSaleDTO
    {
        public DateTimeOffset Timestamp { get; set; }

        public long Price { get; set; }

        public int Quantity { get; set; }
    }
}
