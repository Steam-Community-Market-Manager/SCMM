using System.Text.Json.Serialization;

namespace SCMM.Market.SkinMarketgg.Client
{
    public class SkinMarketGGItem
    {
        [JsonPropertyName("assetId")]
        public string AssetId { get; set; }

        [JsonPropertyName("botId")]
        public int BotId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("imageUrl")]
        public string ImageUrl { get; set; }

        [JsonPropertyName("color")]
        public string Color { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("priceCents")]
        public long PriceCents { get; set; }
    }
}
