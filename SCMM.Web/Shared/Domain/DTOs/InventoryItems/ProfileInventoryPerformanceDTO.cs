using System;
using System.Collections.Generic;

namespace SCMM.Web.Shared.Domain.DTOs.InventoryItems
{
    public class ProfileInventoryPerformanceDTO
    {
        public string SteamId { get; set; }

        public IDictionary<string, long> ValueHistoryGraph { get; set; }

        public IDictionary<string, long> ProfitHistoryGraph { get; set; }
    }
}
