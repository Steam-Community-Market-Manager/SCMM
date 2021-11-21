using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.WebApi.Models
{
    public class ItemDefinition
    {
        [JsonPropertyName("appid")]
        public ulong AppId { get; set; }

        [JsonPropertyName("itemdefid")]
        public ulong ItemDefId { get; set; }

        [JsonPropertyName("Timestamp")]
        public string Timestamp { get; set; }
      
        [JsonPropertyName("modified")]
        public string Modified { get; set; }
        
        [JsonPropertyName("date_created")]
        public string DateCreated { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("display_type")]
        public string DisplayType { get; set; }

        [JsonPropertyName("market_hash_name")]
        public string MarketHashName { get; set; }

        [JsonPropertyName("market_name")]
        public string MarketName { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("price_category")]
        public string PriceCategory { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("promo")]
        public string Promo { get; set; }

        [JsonPropertyName("exchange")]
        public string Exchange { get; set; }

        [JsonPropertyName("background_color")]
        public string BackgroundColor { get; set; }

        [JsonPropertyName("icon_url")]
        public string IconUrl { get; set; }

        [JsonPropertyName("icon_url_large")]
        public string IconUrlLarge { get; set; }

        [JsonPropertyName("name_color")]
        public string NameColor { get; set; }

        [JsonPropertyName("store_tags")]
        public string StoreTags { get; set; }

        [JsonPropertyName("tags")]
        public string Tags { get; set; }

        [JsonPropertyName("tradable")]
        public bool Tradable { get; set; }

        [JsonPropertyName("marketable")]
        public bool Marketable { get; set; }

        [JsonPropertyName("commodity")]
        public bool Commodity { get; set; }

        [JsonPropertyName("itemshortname")]
        public string ItemShortName { get; set; }

        [JsonPropertyName("workshopid")]
        public ulong WorkshopId { get; set; }

        [JsonPropertyName("workshopdownload")]
        public string WorkshopDownload { get; set; }
    }
}
