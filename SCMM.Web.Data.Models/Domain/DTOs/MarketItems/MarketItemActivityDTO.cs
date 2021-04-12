using System;

namespace SCMM.Web.Data.Models.Domain.DTOs.MarketItems
{
    public class MarketItemActivityDTO
    {
        public DateTimeOffset Timestamp { get; set; }

        public long Movement { get; set; }
    }
}
