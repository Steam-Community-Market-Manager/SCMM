namespace SCMM.Steam.Shared
{
    public static class SteamConstants
    {
        public const string SteamStoreUrl = "https://store.steampowered.com/";
        public const string SteamStoreLoginUrl = "https://store.steampowered.com/login/";
        public const string SteamCommunityUrl = "https://steamcommunity.com/";
        public const string SteamCommunityMarketUrl = "https://steamcommunity.com/market/";
        public const string SteamCommunityAssetUrl = "https://steamcommunity-a.akamaihd.net/";

        public const string DefaultContextId = "2";

        public const decimal DefaultSteamFeeMultiplier = 0.05m;
        public const decimal DefaultPublisherFeeMultiplier = 0.100000001490116119m;

        public const string SteamActionViewWorkshopItem = "#Workshop_Commerce_ViewItem";
        public const string SteamActionViewWorkshopItemRegex = @"filedetails\/\?id=(\d+)";
    }
}
