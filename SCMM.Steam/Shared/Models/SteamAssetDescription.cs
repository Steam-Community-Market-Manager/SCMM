using Newtonsoft.Json;

namespace SCMM.Steam.Shared.Models
{
    public class SteamAssetDescription
    {
        [JsonProperty("classid")]
        public string ClassId { get; set; }

        [JsonProperty("market_name")]
        public string MarketName { get; set; }

        [JsonProperty("market_hash_name")]
        public string MarketNameHash { get; set; }

        [JsonProperty("marketable")]
        public bool Marketable { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_color")]
        public string NameColour { get; set; }

        [JsonProperty("background_color")]
        public string BackgroundColour { get; set; }

        [JsonProperty("icon_url")]
        public string IconUrl { get; set; }

        [JsonProperty("icon_url_large")]
        public string IconUrlLarge { get; set; }
    }
}
