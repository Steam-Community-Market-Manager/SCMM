using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Workshop.Models
{
    public class SteamWorkshopFileManifestGroupFloats
    {
        [JsonPropertyName("_Cutoff")]
        public decimal Cutoff { get; set; }

        [JsonPropertyName("_BumpScale")]
        public decimal BumpScale { get; set; }

        [JsonPropertyName("_Glossiness")]
        public decimal Glossiness { get; set; }

        [JsonPropertyName("_OcclusionStrength")]
        public decimal OcclusionStrength { get; set; }

        [JsonPropertyName("_MicrofiberFuzzIntensity")]
        public decimal MicrofiberFuzzIntensity { get; set; }

        [JsonPropertyName("_MicrofiberFuzzScatter")]
        public decimal MicrofiberFuzzScatter { get; set; }

        [JsonPropertyName("_MicrofiberFuzzOcclusion")]
        public decimal MicrofiberFuzzOcclusion { get; set; }

        [JsonPropertyName("_DirtAmount")]
        public decimal DirtAmount { get; set; }
    }
}
