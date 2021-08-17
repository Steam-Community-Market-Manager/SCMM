using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamAppFilter
    {
        [JsonProperty("appid")]
        public ulong AppId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("localized_name")]
        public string Localized_Name { get; set; }

        [JsonProperty("tags")]
        public Dictionary<string, SteamAppFilterTag> Tags { get; set; }
    }
}
