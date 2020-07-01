using System;

namespace SCMM.Web.Shared.Domain.DTOs.InventoryItems
{
    public class UpdateInventoryItemPriceCommand
    {
        public Guid CurrencyId { get; set; }

        public string Price { get; set; }
    }
}
