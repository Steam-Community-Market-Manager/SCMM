using System.Text.Json.Serialization;

namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemDescriptionDTO : IItemDescription, ICanBeFiltered
    {
        public ulong Id { get; set; }

        public ulong AppId { get; set; }

        public string Name { get; set; }

        public string ItemType { get; set; }

        public bool? HasGlow { get; set; }

        public string DominantColour { get; set; }

        public string BackgroundColour { get; set; }

        public string ForegroundColour { get; set; }

        public string IconUrl { get; set; }

        public DateTimeOffset? TimeCreated { get; set; }

        public DateTimeOffset? TimeAccepted { get; set; }

        [JsonIgnore]
        public string[] Filters => new[]
        {
            Id.ToString(), Name, ItemType
        };
    }
}
