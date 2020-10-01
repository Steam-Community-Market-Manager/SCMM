namespace SCMM.Web.Shared.Domain.DTOs.InventoryItems
{
    public class ProfileInventoryItemSummaryDTO : IFilterableItem
    {
        public InventoryMarketItemDTO Item { get; set; }

        public string Name => Item?.Name;

        public int Quantity { get; set; }
    }
}
