using System.Text.Json.Serialization;

namespace SCMM.Market.ShadowPay.Client
{
    public class ShadowPayItem
    {
        [JsonPropertyName("steam_market_hash_name")]
        public string MarketHashName { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }
    }
}