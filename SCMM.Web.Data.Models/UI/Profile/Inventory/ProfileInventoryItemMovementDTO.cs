using SCMM.Web.Data.Models.UI.Item;
using System.Text.Json.Serialization;

namespace SCMM.Web.Data.Models.UI.Profile.Inventory
{
    public class ProfileInventoryItemMovementDTO : ICanBeFiltered
    {
        public ItemDescriptionDTO Item { get; set; }

        public DateTimeOffset MovementTime { get; set; }

        public long Movement { get; set; }

        public long Value { get; set; }

        public long Quantity { get; set; }

        [JsonIgnore]
        public string[] Filters => Item?.Filters;
    }
}
