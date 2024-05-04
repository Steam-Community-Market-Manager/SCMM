using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SCMM.Market.CSDeals.Client
{
    public class CSDealsResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("response")]
        public JsonNode Response { get; set; }
    }
}
