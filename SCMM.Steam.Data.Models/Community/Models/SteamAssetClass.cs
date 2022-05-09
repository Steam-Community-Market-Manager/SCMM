using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamAssetClass
    {
        [JsonPropertyName("classid")]
        public ulong ClassId { get; set; }

        [JsonPropertyName("contextid")]
        public ulong ContextId { get; set; }

        [JsonPropertyName("instanceid")]
        public ulong InstanceId { get; set; }

        [JsonPropertyName("appid")]
        public ulong AppId { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("market_name")]
        public string MarketName { get; set; }

        [JsonPropertyName("market_hash_name")]
        public string MarketHashName { get; set; }

        [JsonPropertyName("marketable")]
        public string Marketable { get; set; }

        [JsonPropertyName("market_marketable_restriction")]
        public string MarketMarketableRestriction { get; set; }

        [JsonPropertyName("market_tradable_restriction")]
        public string MarketTradableRestriction { get; set; }

        [JsonPropertyName("owner_descriptions")]
        public List<SteamAssetClassOwnerDescription> OwnerDescriptions { get; set; }

        [JsonPropertyName("tradable")]
        public string Tradable { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("name_color")]
        public string NameColor { get; set; }

        [JsonPropertyName("background_color")]
        public string BackgroundColor { get; set; }

        [JsonPropertyName("icon_url")]
        public string IconUrl { get; set; }

        [JsonPropertyName("icon_url_large")]
        public string IconUrlLarge { get; set; }

        [JsonPropertyName("descriptions")]
        public List<SteamAssetClassDescription> Descriptions { get; set; }

        [JsonPropertyName("commodity")]
        public string Commodity { get; set; }

        [JsonPropertyName("actions")]
        public List<SteamAssetClassAction> Actions { get; set; }

        [JsonPropertyName("tags")]
        public List<SteamAssetClassTag> Tags { get; set; }
    }
}
