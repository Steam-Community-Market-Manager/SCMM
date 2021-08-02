using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models.Workshop.Models
{
    public class SteamWorkshopFileManifestGroupTextures
    {
        [JsonProperty("_MainTex")]
        public string MainTex { get; set; }

        [JsonProperty("_OcclusionMap")]
        public string OcclusionMap { get; set; }

        [JsonProperty("_SpecGlossMap")]
        public string SpecGlossMap { get; set; }

        [JsonProperty("_BumpMap")]
        public string BumpMap { get; set; }

        [JsonProperty("_EmissionMap")]
        public string EmissionMap { get; set; }

        [JsonProperty("_MicrofiberFuzzMask")]
        public string MicrofiberFuzzMask { get; set; }

        [JsonProperty("_DirtColor")]
        public string DirtColor { get; set; }
    }
}
