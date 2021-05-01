using System;

namespace SCMM.Web.Data.Models.Domain.InventoryItems
{
    public class UpdateInventoryItemCommand
    {
        public Guid CurrencyId { get; set; }

        public long BuyPrice { get; set; }
    }
}
