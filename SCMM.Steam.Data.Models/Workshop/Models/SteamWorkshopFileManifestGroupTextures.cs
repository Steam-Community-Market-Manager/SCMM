using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Workshop.Models
{
    public class SteamWorkshopFileManifestGroupTextures
    {
        [JsonPropertyName("_MainTex")]
        public string MainTex { get; set; }

        [JsonPropertyName("_OcclusionMap")]
        public string OcclusionMap { get; set; }

        [JsonPropertyName("_SpecGlossMap")]
        public string SpecGlossMap { get; set; }

        [JsonPropertyName("_BumpMap")]
        public string BumpMap { get; set; }

        [JsonPropertyName("_EmissionMap")]
        public string EmissionMap { get; set; }

        [JsonPropertyName("_MicrofiberFuzzMask")]
        public string MicrofiberFuzzMask { get; set; }

        [JsonPropertyName("_DirtColor")]
        public string DirtColor { get; set; }
    }
}
