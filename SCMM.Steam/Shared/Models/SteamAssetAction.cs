using Newtonsoft.Json;

namespace SCMM.Steam.Shared.Models
{
    public class SteamAssetAction
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }
    }
}
