using System;

namespace SCMM.Web.Data.Models.Domain.DTOs.InventoryItems
{
    public class UpdateInventoryItemCommand
    {
        public Guid CurrencyId { get; set; }

        public long BuyPrice { get; set; }
    }
}
