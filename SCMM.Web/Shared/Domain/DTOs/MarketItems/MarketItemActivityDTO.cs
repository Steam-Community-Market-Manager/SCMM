using System;

namespace SCMM.Web.Shared.Domain.DTOs.MarketItems
{
    public class MarketItemActivityDTO
    {
        public DateTimeOffset Timestamp { get; set; }

        public long Movement { get; set; }
    }
}
