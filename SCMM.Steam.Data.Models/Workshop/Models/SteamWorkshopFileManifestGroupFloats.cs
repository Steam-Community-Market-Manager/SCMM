using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models.Workshop.Models
{
    public class SteamWorkshopFileManifestGroupFloats
    {
        [JsonProperty("_Cutoff")]
        public decimal _Cutoff { get; set; }

        [JsonProperty("_BumpScale")]
        public decimal BumpScale { get; set; }

        [JsonProperty("_Glossiness")]
        public decimal Glossiness { get; set; }

        [JsonProperty("_OcclusionStrength")]
        public decimal OcclusionStrength { get; set; }

        [JsonProperty("_MicrofiberFuzzIntensity")]
        public decimal MicrofiberFuzzIntensity { get; set; }

        [JsonProperty("_MicrofiberFuzzScatter")]
        public decimal MicrofiberFuzzScatter { get; set; }

        [JsonProperty("_MicrofiberFuzzOcclusion")]
        public decimal MicrofiberFuzzOcclusion { get; set; }

        [JsonProperty("_DirtAmount")]
        public decimal DirtAmount { get; set; }
    }
}
