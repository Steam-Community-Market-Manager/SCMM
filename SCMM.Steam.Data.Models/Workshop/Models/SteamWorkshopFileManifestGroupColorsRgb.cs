using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Workshop.Models
{
    public class SteamWorkshopFileManifestGroupColorsRgb
    {
        [JsonPropertyName("r")]
        public decimal R { get; set; }

        [JsonPropertyName("g")]
        public decimal G { get; set; }

        [JsonPropertyName("b")]
        public decimal B { get; set; }
    }
}
