using Newtonsoft.Json;
using System.Collections.Generic;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamAssetFilter
    {
        [JsonProperty("appid")]
        public string AppId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("localized_name")]
        public string Localized_Name { get; set; }

        [JsonProperty("tags")]
        public Dictionary<string, SteamAssetTag> Tags { get; set; }
    }
}
