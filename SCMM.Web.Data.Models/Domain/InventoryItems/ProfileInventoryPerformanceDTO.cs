using System.Collections.Generic;

namespace SCMM.Web.Data.Models.Domain.InventoryItems
{
    public class ProfileInventoryPerformanceDTO
    {
        public IDictionary<string, long> ValueHistoryGraph { get; set; }

        public IDictionary<string, long> ProfitHistoryGraph { get; set; }
    }
}
