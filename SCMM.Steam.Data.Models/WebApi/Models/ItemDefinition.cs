using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.WebApi.Models
{
    /// <summary>
    /// https://partner.steamgames.com/doc/features/inventory/schema#Overview
    /// </summary>
    public class ItemDefinition
    {
        [JsonPropertyName("appid")]
        public ulong AppId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("display_type")]
        public string DisplayType { get; set; }

        [JsonPropertyName("itemdefid")]
        public ulong ItemDefId { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("bundle")]
        public string Bundle { get; set; }

        [JsonPropertyName("promo")]
        public string Promo { get; set; }

        [JsonPropertyName("drop_start_time")]
        public string DropStartTime { get; set; }

        [JsonPropertyName("exchange")]
        public string Exchange { get; set; }

        [JsonPropertyName("price")]
        public string Price { get; set; }

        [JsonPropertyName("price_category")]
        public string PriceCategory { get; set; }

        [JsonPropertyName("background_color")]
        public string BackgroundColor { get; set; }

        [JsonPropertyName("name_color")]
        public string NameColor { get; set; }

        [JsonPropertyName("icon_url")]
        public string IconUrl { get; set; }

        [JsonPropertyName("icon_url_large")]
        public string IconUrlLarge { get; set; }

        [JsonPropertyName("marketable")]
        public bool Marketable { get; set; }

        [JsonPropertyName("tradable")]
        public bool Tradable { get; set; }

        [JsonPropertyName("tags")]
        public string Tags { get; set; }

        [JsonPropertyName("store_tags")]
        public string StoreTags { get; set; }

        [JsonPropertyName("store_images")]
        public string StoreImages { get; set; }

        [JsonPropertyName("game_only")]
        public bool GameOnly { get; set; }

        [JsonPropertyName("hidden")]
        public bool Hidden { get; set; }

        [JsonPropertyName("store_hidden")]
        public bool StoreHidden { get; set; }

        [JsonPropertyName("use_bundle_price")]
        public bool UseBundlePrice { get; set; }

        [JsonPropertyName("auto_stack")]
        public bool AutoStack { get; set; }

        #region Common extended properties

        [JsonPropertyName("Timestamp")]
        public string Timestamp { get; set; }

        [JsonPropertyName("modified")]
        public string Modified { get; set; }

        [JsonPropertyName("date_created")]
        public string DateCreated { get; set; }

        [JsonPropertyName("workshopid")]
        public ulong WorkshopId { get; set; }

        [JsonPropertyName("workshopdownload")]
        public string WorkshopDownload { get; set; }

        [JsonPropertyName("market_name")]
        public string MarketName { get; set; }

        [JsonPropertyName("market_hash_name")]
        public string MarketHashName { get; set; }

        [JsonPropertyName("commodity")]
        public bool Commodity { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        #endregion

        #region Rust extended properties

        [JsonPropertyName("itemshortname")]
        public string RustItemShortName { get; set; }

        #endregion

        #region Unturned extended properties

        [JsonPropertyName("scraps")]
        public string UnturnedScraps { get; set; }

        [JsonPropertyName("item_id")]
        public string UnturnedItemId { get; set; }

        [JsonPropertyName("item_skin")]
        public string UnturnedItemSkin { get; set; }

        [JsonPropertyName("item_effect")]
        public string UnturnedItemEffect { get; set; }

        [JsonPropertyName("vehicle_id")]
        public string UnturnedVehicleId { get; set; }

        #endregion
    }
}
