using SCMM.Web.Data.Models.UI.Item;

namespace SCMM.Web.Data.Models.UI.Profile.Inventory
{
    public class ProfileInventoryItemMovementDTO
    {
        public IEnumerable<ItemDescriptionDTO> Items { get; set; }

        public IDictionary<DateTimeOffset, Dictionary<ulong, long>> MarketMovement { get; set; }
    }
}
