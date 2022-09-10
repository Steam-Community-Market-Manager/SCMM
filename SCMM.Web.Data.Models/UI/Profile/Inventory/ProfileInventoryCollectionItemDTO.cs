using SCMM.Web.Data.Models.UI.Item;
using System.Text.Json.Serialization;

namespace SCMM.Web.Data.Models.UI.Profile.Inventory
{
    public class ProfileInventoryCollectionItemDTO : ICanBeFiltered
    {
        public ItemDescriptionWithPriceDTO Item { get; set; }

        public bool IsOwned { get; set; }

        [JsonIgnore]
        public string[] Filters => Item?.Filters;
    }
}
