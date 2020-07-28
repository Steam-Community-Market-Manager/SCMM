using System;

namespace SCMM.Web.Shared.Domain.DTOs.InventoryItems
{
    public class ProfileInventoryTotalsDTO
    {
        public int TotalItems { get; set; }

        public long TotalInvested { get; set; }

        public long TotalMarketValue { get; set; }

        public long TotalResellValue { get; set; }
    }
}
