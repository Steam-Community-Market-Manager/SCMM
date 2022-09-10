using System.Text.Json.Serialization;

namespace SCMM.Market.SkinsMonkey.Client
{
    public class SkinsMonkeyItem
    {
        [JsonPropertyName("app_id")]
        public ulong AppId { get; set; }

        [JsonPropertyName("market_hash_name")]
        public string MarketHashName { get; set; }

        [JsonPropertyName("price_trade")]
        public long PriceTrade { get; set; }

        [JsonPropertyName("price_cash")]
        public long PriceCash { get; set; }

        [JsonPropertyName("stock")]
        public int Stock { get; set; }
    }
}
