using System.Text.Json.Serialization;

namespace SCMM.Market.SkinSwap.Client
{
    public class SkinSwapItem
    {
        [JsonPropertyName("assetId")]
        public string AssetId { get; set; }

        [JsonPropertyName("classid")]
        public string ClassId { get; set; }

        [JsonPropertyName("market_name")]
        public string MarketName { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        // "price * 2.0202" = price listed on website
        public decimal PriceListed => (Price * 2.0202m);

        [JsonPropertyName("max")]
        public int Max { get; set; }
    }
}
