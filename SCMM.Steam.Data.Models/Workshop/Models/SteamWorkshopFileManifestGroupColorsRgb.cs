using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models.Workshop.Models
{
    public class SteamWorkshopFileManifestGroupColorsRgb
    {
        [JsonProperty("r")]
        public decimal R { get; set; }

        [JsonProperty("g")]
        public decimal G { get; set; }

        [JsonProperty("b")]
        public decimal B { get; set; }
    }
}
