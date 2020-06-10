namespace SCMM.Steam.Shared
{
    public static class SteamConstants
    {
        public const string SteamStoreUrl = "https://store.steampowered.com/";
        public const string SteamCommunityUrl = "https://steamcommunity.com/";
        public const string SteamCommunityAssetUrl = "https://steamcommunity-a.akamaihd.net/";
        public const string SteamWebApiUrl = "https://api.steampowered.com/";

        public const string SteamActionViewWorkshopItem = "#Workshop_Commerce_ViewItem";
        public const string SteamActionViewWorkshopItemRegex = @"filedetails\/\?id=(\d+)";

        public const string DefaultContextId = "2";
    }
}
