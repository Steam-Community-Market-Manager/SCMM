using System.Text.Json.Serialization;

namespace SCMM.Market.Waxpeer.Client
{
    public class WaxpeerItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("min")]
        public long Min { get; set; }
    }
}