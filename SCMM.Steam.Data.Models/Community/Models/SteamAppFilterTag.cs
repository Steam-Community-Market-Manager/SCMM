using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamAppFilterTag
    {
        [JsonProperty("localized_name")]
        public string Localized_Name { get; set; }

        [JsonProperty("matches")]
        public string Matches { get; set; }
    }
}
