namespace SCMM.Steam.Shared
{
    public static class SteamConstants
    {
        public const string SteamStoreUrl = "https://store.steampowered.com/";
        public const string SteamCommunityUrl = "https://steamcommunity.com/";
        public const string SteamCommunityAssetUrl = "https://steamcommunity-a.akamaihd.net/";
        public const string SteamWebApiUrl = "https://api.steampowered.com/";

        public const string SteamAssetTagCategory = "steamcat";
        public const string SteamAssetTagItemType = "itemclass";
        public const string SteamAssetTagWorkshop = "workshop";
        public const string SteamAssetTagCreator = "creator";
        public const string SteamAssetTagSet = "set";
        public const string SteamAssetTagAcceptedYear = "accepted.year";
        public const string SteamAssetTagAcceptedWeek = "accepted.week";

        public const string SteamProfileIdRegex = @"id\/(.*)\/";

        public static readonly string[] SteamIgnoredWorkshopTags = { "Skin", "Version3" };

        public const string SteamActionViewWorkshopItem = "#Workshop_Commerce_ViewItem";
        public const string SteamActionViewWorkshopItemRegex = @"filedetails\/\?id=(\d+)";

        public const string SteamStoreItemDef = "item_def_grid_item";
        public const string SteamStoreItemDefName = "item_def_name";
        public const string SteamStoreItemDefLinkRegex = @"detail\/(\d+)";

        public const string SteamMarketListingItemNameIdRegex = @"Market_LoadOrderSpread\((.*)\)";

        public const string DefaultContextId = "2";
    }
}
