using System.Text.Json.Serialization;

namespace SCMM.Market.RustSkins.Client
{
    public class RustSkinsItemListing
    {
        [JsonPropertyName("id")]
        public ulong Id { get; set; }

        [JsonPropertyName("user_id")]
        public ulong UserId { get; set; }

        [JsonPropertyName("market_hash_name")]
        public string MarketHashName { get; set; }

        [JsonPropertyName("assetid")]
        public string AssetId { get; set; }

        [JsonPropertyName("price")]
        public float Price { get; set; }

        [JsonPropertyName("custom_price")]
        public float CustomPrice { get; set; }
    }
}
