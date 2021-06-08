using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamAssetClassTag
    {
        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("internal_name")]
        public string InternalName { get; set; }

        [JsonProperty("localized_category_name")]
        public string LocalizedCategoryName { get; set; }

        [JsonProperty("localized_tag_name")]
        public string LocalizedTagName { get; set; }
    }
}
