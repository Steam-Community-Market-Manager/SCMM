using System.Text.Json.Serialization;

namespace SCMM.Webshare.Client
{
    public class WebshareListProxiesResponseJson
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("next")]
        public string Next { get; set; }

        [JsonPropertyName("previous")]
        public string Previous { get; set; }

        [JsonPropertyName("results")]
        public IEnumerable<WebshareProxyDetails> Results { get; set; }
    }
}