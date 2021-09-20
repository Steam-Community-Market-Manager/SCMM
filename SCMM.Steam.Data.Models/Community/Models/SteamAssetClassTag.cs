using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamAssetClassTag
    {
        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("internal_name")]
        public string InternalName { get; set; }

        [JsonPropertyName("localized_category_name")]
        public string LocalizedCategoryName { get; set; }

        [JsonPropertyName("localized_tag_name")]
        public string LocalizedTagName { get; set; }
    }
}
