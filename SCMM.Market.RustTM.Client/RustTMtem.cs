using System.Text.Json.Serialization;

namespace SCMM.Market.RustTM.Client
{
    public class RustTMItem
    {
        [JsonPropertyName("market_hash_name")]
        public string MarketHashName { get; set; }

        [JsonPropertyName("volume")]
        public int Volume { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }
    }
}
