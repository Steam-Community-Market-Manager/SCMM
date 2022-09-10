using System.Text.Json.Serialization;

namespace SCMM.Web.Data.Models.UI.Profile.Inventory
{
    public class ProfileInventoryCollectionDTO : ICanBeFiltered
    {
        public string Name { get; set; }

        public string CreatorName { get; set; }

        public string CreatorAvatarUrl { get; set; }

        public ProfileInventoryCollectionItemDTO[] Items { get; set; }

        [JsonIgnore]
        public string[] Filters => Items
            .SelectMany(x => x.Filters)
            .Union(new[] {
                Name, CreatorName
            })
            .ToArray();
    }
}
