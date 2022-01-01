using System.Text.Json.Serialization;

namespace SCMM.Market.Skinport.Client
{
    public class SkinportMarketItem
    {
        public const string SaleStatusListed = "listed";
        public const string SaleTypePublic = "public";

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("saleId")]
        public long SaleId { get; set; }

        [JsonPropertyName("appid")]
        public long AppId { get; set; }

        [JsonPropertyName("classid")]
        public string ClassId { get; set; }

        [JsonPropertyName("steamId")]
        public string SteamId { get; set; }

        [JsonPropertyName("assetid")]
        public string AssetId { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("marketName")]
        public string MarketName { get; set; }

        [JsonPropertyName("marketHashName")]
        public string MarketHashName { get; set; }

        [JsonPropertyName("suggestedPrice")]
        public long SuggestedPrice { get; set; }

        [JsonPropertyName("salePrice")]
        public long SalePrice { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("saleStatus")]
        public string SaleStatus { get; set; }

        [JsonPropertyName("saleType")]
        public string SaleType { get; set; }
    }
}
