using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamAppFilter
    {
        [JsonPropertyName("appid")]
        public ulong AppId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("localized_name")]
        public string Localized_Name { get; set; }

        [JsonPropertyName("tags")]
        public Dictionary<string, SteamAppFilterTag> Tags { get; set; }
    }
}
