using System.Text.Json.Serialization;

namespace SCMM.Market.CSTrade.Client
{
    public class CSTradeInventoryItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("app_id")]
        public string AppId { get; set; }

        [JsonPropertyName("market_hash_name")]
        public string MarketHashName { get; set; }

        [JsonPropertyName("price")]
        public decimal? Price { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("bot")]
        public string Bot { get; set; }

        [JsonPropertyName("bot_id")]
        public string BotId { get; set; }
    }
}
