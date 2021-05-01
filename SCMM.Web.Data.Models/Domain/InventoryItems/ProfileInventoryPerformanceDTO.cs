using System.Collections.Generic;
using SCMM.Web.Data.Models.Domain.InventoryItems;

namespace SCMM.Web.Data.Models.Domain.InventoryItems
{
    public class ProfileInventoryPerformanceDTO
    {
        public IDictionary<string, long> ValueHistoryGraph { get; set; }

        public IDictionary<string, long> ProfitHistoryGraph { get; set; }
    }
}
