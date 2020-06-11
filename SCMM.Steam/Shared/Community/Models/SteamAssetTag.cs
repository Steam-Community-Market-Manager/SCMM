using Newtonsoft.Json;

namespace SCMM.Steam.Shared.Community.Models
{
    public class SteamAssetTag
    {
        [JsonProperty("localized_name")]
        public string Localized_Name { get; set; }

        [JsonProperty("matches")]
        public string Matches { get; set; }
    }
}
