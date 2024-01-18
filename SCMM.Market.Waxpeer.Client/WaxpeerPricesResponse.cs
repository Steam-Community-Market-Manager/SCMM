using System.Text.Json.Serialization;

namespace SCMM.Market.Waxpeer.Client
{
    public class WaxpeerPricesResponse
    {
        [JsonPropertyName("items")]
        public IEnumerable<WaxpeerItem> Items { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }
}
