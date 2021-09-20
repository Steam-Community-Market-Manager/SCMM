using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamAppFilterTag
    {
        [JsonPropertyName("localized_name")]
        public string Localized_Name { get; set; }

        [JsonPropertyName("matches")]
        public string Matches { get; set; }
    }
}
