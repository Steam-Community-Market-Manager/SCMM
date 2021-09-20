using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Workshop.Models
{
    public class SteamWorkshopFileManifestGroupColors
    {
        [JsonPropertyName("_Color")]
        public SteamWorkshopFileManifestGroupColorsRgb Color { get; set; }

        [JsonPropertyName("_SpecColor")]
        public SteamWorkshopFileManifestGroupColorsRgb SpecColor { get; set; }

        [JsonPropertyName("_EmissionColor")]
        public SteamWorkshopFileManifestGroupColorsRgb EmissionColor { get; set; }

        [JsonPropertyName("_MicrofiberFuzzColor")]
        public SteamWorkshopFileManifestGroupColorsRgb MicrofiberFuzzColor { get; set; }
    }
}
