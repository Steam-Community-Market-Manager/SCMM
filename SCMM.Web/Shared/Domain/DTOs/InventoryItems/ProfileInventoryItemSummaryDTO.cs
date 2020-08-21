using System;

namespace SCMM.Web.Shared.Domain.DTOs.InventoryItems
{
    public class ProfileInventoryItemSummaryDTO
    {
        public InventoryMarketItemDTO Item { get; set; }

        public int Quantity { get; set; }
    }
}
