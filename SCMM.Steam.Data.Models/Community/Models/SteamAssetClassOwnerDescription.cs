using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamAssetClassOwnerDescription
    {
        [JsonPropertyName("value")]
        public string Value { get; set; }
    }
}
