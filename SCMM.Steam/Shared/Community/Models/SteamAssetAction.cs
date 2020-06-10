using Newtonsoft.Json;

namespace SCMM.Steam.Shared.Community.Models
{
    public class SteamAssetAction
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }
    }
}
