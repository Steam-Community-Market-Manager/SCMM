using System;

namespace SCMM.Web.Data.Models.Domain.MarketItems
{
    public class MarketItemActivityDTO
    {
        public DateTimeOffset Timestamp { get; set; }

        public long Movement { get; set; }
    }
}
