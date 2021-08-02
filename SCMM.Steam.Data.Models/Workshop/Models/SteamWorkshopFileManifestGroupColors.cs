using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models.Workshop.Models
{
    public class SteamWorkshopFileManifestGroupColors
    {
        [JsonProperty("_Color")]
        public SteamWorkshopFileManifestGroupColorsRgb Color { get; set; }

        [JsonProperty("_SpecColor")]
        public SteamWorkshopFileManifestGroupColorsRgb SpecColor { get; set; }

        [JsonProperty("_EmissionColor")]
        public SteamWorkshopFileManifestGroupColorsRgb EmissionColor { get; set; }

        [JsonProperty("_MicrofiberFuzzColor")]
        public SteamWorkshopFileManifestGroupColorsRgb MicrofiberFuzzColor { get; set; }
    }
}
