using System.Text.Json.Serialization;

namespace SCMM.Market.RapidSkins.Client
{
    public class RapidSkinsInventoryResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public IDictionary<string, IEnumerable<RapidSkinsItem>> Data { get; set; }
    }
}
