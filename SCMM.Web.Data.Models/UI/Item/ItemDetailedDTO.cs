using SCMM.Steam.Data.Models.Enums;
using SCMM.Web.Data.Models.UI.Profile;
using System;
using System.Collections.Generic;

namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemDetailedDTO : IItemDescription
    {
        #region Asset Description

        public ulong Id { get; set; }

        public ulong AppId { get; set; }

        public SteamAssetDescriptionType AssetType { get; set; }

        public ulong? WorkshopFileId { get; set; }

        public ProfileDTO Creator { get; set; }

        public string ItemType { get; set; }

        public string ItemCollection { get; set; }

        public string Name { get; set; }

        public ulong? NameId { get; set; }

        public string Description { get; set; }

        public IDictionary<string, string> Tags { get; set; }

        public string BackgroundColour { get; set; }

        public string ForegroundColour { get; set; }

        public string IconUrl { get; set; }

        public string IconLargeUrl { get; set; }

        public string ImageUrl { get; set; }

        public long? CurrentSubscriptions { get; set; }

        public long? TotalSubscriptions { get; set; }

        public bool IsCommodity { get; set; }

        public bool IsMarketable { get; set; }

        public int? MarketableRestrictionDays { get; set; }

        public bool IsTradable { get; set; }

        public int? TradableRestrictionDays { get; set; }

        public bool IsCraftingComponent { get; set; }

        public bool IsCraftable { get; set; }

        public IDictionary<string, int> CraftingComponents { get; set; }

        public bool IsBreakable { get; set; }

        public IDictionary<string, int> BreaksIntoComponents { get; set; }

        public bool IsBanned { get; set; }

        public string BanReason { get; set; }

        public DateTimeOffset? TimeCreated { get; set; }

        public DateTimeOffset? TimeUpdated { get; set; }

        public DateTimeOffset? TimeAccepted { get; set; }

        public DateTimeOffset? TimeRefreshed { get; set; }

        public PriceType? BuyNowFrom { get; set; }

        public long? BuyNowPrice { get; set; }

        public string BuyNowUrl { get; set; }

        #endregion

        #region Market Item

        public string MarketId { get; set; }

        public int? MarketBuyOrderCount { get; set; }

        public long? MarketBuyPrice { get; set; }

        public int? MarketSellOrderCount { get; set; }

        public long? MarketSellPrice { get; set; }

        public long? MarketSellTax { get; set; }

        public long? Market1hrSales { get; set; }

        public long? Market1hrValue { get; set; }

        public long? Market24hrSales { get; set; }

        public long? Market24hrValue { get; set; }

        public bool IsAvailableOnMarket { get; set; }

        #endregion

        #region Store Item 

        public long? StoreId { get; set; }

        public long? StorePrice { get; set; }

        public bool IsAvailableOnStore { get; set; }

        #endregion
    }
}
