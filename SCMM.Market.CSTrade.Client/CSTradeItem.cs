using System.Text.Json.Serialization;

namespace SCMM.Market.CSTrade.Client
{
    public class CSTradeItem
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("app_id")]
        public long AppId { get; set; }

        [JsonPropertyName("market_hash_name")]
        public string MarketHashName { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("bot")]
        public string Bot { get; set; }

        [JsonPropertyName("bot_id")]
        public string BotId { get; set; }
    }
}
