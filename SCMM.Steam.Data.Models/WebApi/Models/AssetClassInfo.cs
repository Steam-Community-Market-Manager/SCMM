using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.WebApi.Models
{
    public class AssetClassInfo
    {
        [JsonPropertyName("icon_url")]
        public string IconUrl { get; set; }

        [JsonPropertyName("icon_url_large")]
        public string IconUrlLarge { get; set; }

        [JsonPropertyName("icon_drag_url")]
        public string IconDragUrl { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("market_hash_name")]
        public string MarketHashName { get; set; }

        [JsonPropertyName("market_name")]
        public string MarketName { get; set; }

        [JsonPropertyName("name_color")]
        public string NameColor { get; set; }

        [JsonPropertyName("background_color")]
        public string BackgroundColor { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("tradable")]
        public string Tradable { get; set; }

        [JsonPropertyName("marketable")]
        public string Marketable { get; set; }

        [JsonPropertyName("commodity")]
        public string Commodity { get; set; }

        [JsonPropertyName("market_tradable_restriction")]
        public string MarketTradableRestriction { get; set; }

        [JsonPropertyName("market_marketable_restriction")]
        public string MarketMarketableRestriction { get; set; }

        [JsonPropertyName("fraud_warnings")]
        public string FraudWarnings { get; set; }

        [JsonPropertyName("descriptions")]
        public IDictionary<string, AssetClassDescription> Descriptions { get; set; }

        [JsonPropertyName("actions")]
        public IDictionary<string, AssetClassAction> Actions { get; set; }

        [JsonPropertyName("market_actions")]
        public IDictionary<string, AssetClassMarketAction> MarketActions { get; set; }

        [JsonPropertyName("tags")]
        public IDictionary<string, AssetClassTag> Tags { get; set; }

        [JsonPropertyName("classid")]
        public string ClassId { get; set; }
    }
}
