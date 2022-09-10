using System.Text.Json.Serialization;

namespace SCMM.Market.Skinport.Client
{
    public class SkinportItem
    {
        [JsonPropertyName("market_hash_name")]
        public string MarketHashName { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("suggested_price")]
        public float? SuggestedPrice { get; set; }

        [JsonPropertyName("item_page")]
        public string ItemPage { get; set; }

        [JsonPropertyName("market_page")]
        public string MarketPage { get; set; }

        [JsonPropertyName("min_price")]
        public float? MinPrice { get; set; }

        [JsonPropertyName("max_price")]
        public float? MaxPrice { get; set; }

        [JsonPropertyName("mean_price")]
        public float? MeanPrice { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public long UpdatedAt { get; set; }
    }
}
