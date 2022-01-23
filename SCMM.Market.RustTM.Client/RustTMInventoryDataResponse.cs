using System.Text.Json.Serialization;

namespace SCMM.Market.RustTM.Client
{
    public class RustTMInventoryDataResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("time")]
        public ulong Time { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("items")]
        public IEnumerable<RustTMItem> Items { get; set; }
    }
}
