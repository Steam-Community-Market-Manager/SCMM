using System.Text.Json.Serialization;

namespace SCMM.Market.SkinSerpent.Client
{
    public class SkinSerpentSkin
    {
        [JsonPropertyName("appid")]
        public long AppId { get; set; }

        [JsonPropertyName("assetid")]
        public string AssetId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("market_name")]
        public string MarketName { get; set; }

        [JsonPropertyName("market_hash_name")]
        public string MarketHashName { get; set; }

        [JsonPropertyName("price")]
        public int Price { get; set; }

    }
}
