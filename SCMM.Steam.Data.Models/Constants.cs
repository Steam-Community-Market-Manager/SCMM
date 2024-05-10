﻿namespace SCMM.Steam.Data.Models
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
        public const string SteamAssetTagStore = "store";

        public static string SteamWorkshopTagSkin = "Skin";
        public static readonly string[] SteamIgnoredWorkshopTags = {
            "Skin", "Version3"
        };

        public const string SteamLoginClaimSteamIdRegex = @"\/openid\/id\/([^\/]*)";
        public const string SteamProfileUrlCustomUrlRegex = @"\/id\/([^\/]*)";
        public const string SteamProfileUrlSteamId64Regex = @"\/profiles\/([^\/]*)";

        public const string SteamAssetClassTypeWorkshopItem = "Workshop Item";
        public const string SteamActionViewWorkshopItemId = "#Workshop_Commerce_ViewItem";
        public const string SteamActionViewWorkshopItem = "View Workshop Item";
        public const string SteamActionViewWorkshopItemRegex = @"filedetails\/\?id=(\d+)";

        public const int SteamStoreItemsMax = 30;
        public const string SteamStoreItemDef = "item_def_grid_item";
        public const string SteamStoreItemDefName = "item_def_name";
        public const string SteamStoreItemDefLinkRegex = @"detail\/(\d+)";
        public const string SteamStoreItemDescriptionName = "item_description_snippet";

        public const string SteamMarketListingItemNameIdRegex = @"Market_LoadOrderSpread\((.*)\);";
        public const string SteamMarketListingAssetJsonRegex = @"g_rgAssets\s*=\s*(.*);";

        public const string SteamInventoryItemMarketableAndTradableAfterOwnerDescriptionRegex = @"Tradable \& Marketable After\: \[date\]([0-9]+)\[\/date\]";

        public const ulong SteamAssetValuePublicContextId = 2;

        public const ulong SteamAssetDefaultInstanceId = 0;

        public const string SteamAssetClassDescriptionTypeHtml = "html";
        public const string SteamAssetClassDescriptionTypeBBCode = "bbcode";

        public const int SteamCurrencyIdUSD = 1;
        public const decimal SteamCurrencyExchangeRateUSD = 1.0m;
        public const string SteamCurrencyVLV = "VLV";
        public const string SteamCurrencyUSD = "USD";
        public const string SteamCurrencyEUR = "EUR";
        public const string SteamCurrencyCNY = "CNY";

        public const int SteamDefaultCurrencyId = SteamCurrencyIdUSD;
        public const string SteamDefaultCurrency = SteamCurrencyUSD;
        public const decimal SteamDefaultCurrencyExchangeRate = SteamCurrencyExchangeRateUSD;

        public const string SteamLanguageEnglish = "english";
        public const string SteamDefaultLanguage = SteamLanguageEnglish;

        public const string SteamCountryUS = "US";
        public const string SteamDefaultCountry = "US";

        #region CSGO

        public const ulong CSGOAppId = 730L;

        #endregion

        #region Rust

        public const ulong RustAppId = 252490L;

        public static readonly string[] RustItemNameCommonWords = {
            "Pants", "Vest"
        };

        public const string RustItemTypeNone = "None";
        public const string RustItemTypeResource = "Resource";
        public const string RustItemTypeSkinContainer = "Skin Container";
        public const string RustItemTypeMiscellaneous = "Miscellaneous";
        public const string RustItemTypeUnderwear = "Underwear";
        public const string RustItemTypeBuildingSkin = "Building Skin";

        public const string RustItemShortNameLR300 = "lr300.item";
        public const string RustItemTypeLR300 = "LR300";
        public const string RustItemTypeFurnace = "Furnace";

        #endregion

        #region Unturned

        public const ulong UnturnedAppId = 304930L;

        #endregion

        #region SCMM

        public const string SCMMStoreIdDateFormat = "yyyy-MM-dd-HHmm";

        public const string BlobContainerWorkshopFiles = "workshop-files";
        public const string BlobContainerModels = "models";
        public const string BlobContainerImages = "images";

        public const string BlobMetadataAutoDelete = "autodelete";
        public const string BlobMetadataExpiresOn = "expireson";
        public const string BlobMetadataPublishedFileId = "PublishedFileId";
        public const string BlobMetadataPublishedFileName = "PublishedFileName";
        public const string BlobMetadataIconAnalysed = "IconAnalysed";

        public const string AssetTagAi = "ai";
        public const string AssetTagAiTag = "ai.tag";
        public const string AssetTagAiCaption = "ai.caption";
        public const string AssetTagAiColour = "ai.colour";
        public const string AssetTagAiColourHexRegex = @"ai\.colour\.([0-9A-F]+)";

        public const string LatestSystemUpdatesCacheKey = "latest-system-updates";

        public const string AssetTagItemCategory = "scmmcat";
        public const string AssetTagItemCategoryWearable = "Wearable";
        public const string AssetTagItemCategoryEquipable = "Equipable";
        public const string AssetTagItemCategoryDeployable = "Deployable";
        public const string AssetTagItemCategoryResource = "Resource";
        public const string AssetTagItemCategoryContainer = "Container";

        #endregion
    }
}
