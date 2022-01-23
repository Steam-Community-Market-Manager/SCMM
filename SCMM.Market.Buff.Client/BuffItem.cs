using System.Text.Json.Serialization;

namespace SCMM.Market.Buff.Client
{
    public class BuffItem
    {
        [JsonPropertyName("appid")]
        public ulong AppId { get; set; }

        [JsonPropertyName("id")]
        public ulong Id { get; set; }

        [JsonPropertyName("market_hash_name")]
        public string MarketHashName { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("sell_min_price")]
        public string SellMinPrice { get; set; }

        [JsonPropertyName("sell_reference_price")]
        public string SellReference_Price { get; set; }

        [JsonPropertyName("sell_num")]
        public int SellNum { get; set; }
    }
}