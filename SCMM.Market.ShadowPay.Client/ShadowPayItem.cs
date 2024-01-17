using System.Text.Json.Serialization;

namespace SCMM.Market.ShadowPay.Client
{
    public class ShadowPayItem
    {
        [JsonPropertyName("steam_market_hash_name")]
        public string MarketHashName { get; set; }

        [JsonPropertyName("price")]
        public long Price { get; set; }

        [JsonPropertyName("volume")]
        public int Volume { get; set; }
    }
}