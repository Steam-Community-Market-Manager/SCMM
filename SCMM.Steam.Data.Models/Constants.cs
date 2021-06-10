namespace SCMM.Steam.Data.Models
{
    public static class Constants
    {
        public const string SteamStoreUrl = "https://store.steampowered.com";
        public const string SteamCommunityUrl = "https://steamcommunity.com";
        public const string SteamCommunityAssetUrl = "https://steamcommunity-a.akamaihd.net";
        public const string SteamWebApiUrl = "https://api.steampowered.com";

        public const string SteamAssetTagCategory = "steamcat";
        public const string SteamAssetTagItemType = "itemclass";
        public const string SteamAssetTagWorkshop = "workshop";

        public static readonly string[] SteamIgnoredWorkshopTags = { 
            "Skin", "Version3" 
        };

        public const string SteamLoginClaimSteamIdRegex = @"\/openid\/id\/([^\/]*)";
        public const string SteamProfileUrlProfileIdRegex = @"\/id\/([^\/]*)";
        public const string SteamProfileUrlSteamIdRegex = @"\/profiles\/([^\/]*)";

        public const string SteamAssetClassTypeWorkshopItem = "Workshop Item";
        public const string SteamActionViewWorkshopItemId = "#Workshop_Commerce_ViewItem";
        public const string SteamActionViewWorkshopItem = "View Workshop Item";
        public const string SteamActionViewWorkshopItemRegex = @"filedetails\/\?id=(\d+)";

        public const int SteamStoreItemsMax = 30;
        public const string SteamStoreItemDef = "item_def_grid_item";
        public const string SteamStoreItemDefName = "item_def_name";
        public const string SteamStoreItemDefLinkRegex = @"detail\/(\d+)";
        public const string SteamStoreItemDescriptionName = "item_description_snippet";

        public const string SteamMarketListingItemNameIdRegex = @"ItemActivityTicker.Start\((.*)\);";
        public const string SteamMarketListingAssetJsonRegex = @"g_rgAssets\s*=\s*(.*);";

        public const string SteamAssetClassDescriptionTypeHtml = "html";
        public const string SteamAssetClassDescriptionStripHtmlRegex = @"<[^>]*>";
        public const string SteamAssetClassDescriptionTypeBBCode = "bbcode";
        public const string SteamAssetClassDescriptionStripBBCodeRegex = @"\[[^\]]*\]";

        #region Rust

        public const ulong RustAppId = 252490L;

        public static readonly string[] RustItemNameCommonWords = {
            "Pants", "Vest"
        };

        public const string RustItemTypeResource = "Resource";
        public const string RustItemTypeContainer = "Container";
        public const string RustItemTypeUnique = "Unique Item";
        public const string RustItemTypeUnderwear = "Underwear Item";

        public const string RustItemTypeLR300 = "LR300";

        #endregion

        #region SCMM

        public const string SCMMStoreIdDateFormat = "yyyy-MM-dd-HHmm";

        #endregion
    }
}
