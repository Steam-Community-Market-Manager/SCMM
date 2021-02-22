using System;

namespace SCMM.Web.Shared.Domain.DTOs.InventoryItems
{
    public class UpdateInventoryItemCommand
    {
        public Guid CurrencyId { get; set; }

        public long BuyPrice { get; set; }
    }
}
