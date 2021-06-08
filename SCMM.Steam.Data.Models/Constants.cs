namespace SCMM.Steam.Data.Models
{
    public static class Constants
    {
        public const ulong SteamRustAppId = 252490L;

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

        public static readonly string[] SteamItemNameCommonWords = {
            "Pants", "Vest", "AR"
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

        public const string SteamMarketListingItemNameIdRegex = @"ItemActivityTicker.Start\\((.*)\\);\r\n";
        public const string SteamMarketListingAssetJsonRegex = "g_rgAssets\\s*=\\s*(.*);\r\n";

        public const string SteamAssetClassDescriptionTypeHtml = "html";
        public const string SteamAssetClassDescriptionStripHtmlRegex = @"<[^>]*>";
        public const string SteamAssetClassDescriptionTypeBBCode = "bbcode";
        public const string SteamAssetClassDescriptionStripBBCodeRegex = @"\[[^\]]*\]";

    }
}
