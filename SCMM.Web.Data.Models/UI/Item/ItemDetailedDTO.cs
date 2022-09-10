
using SCMM.Steam.Data.Models.Enums;

namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemDetailedDTO : IItemDescription
    {
        #region Asset Description

        public ulong Id { get; set; }

        public ulong AppId { get; set; }

        public SteamAssetDescriptionType AssetType { get; set; }

        public ulong? WorkshopFileId { get; set; }

        public string WorkshopFileUrl { get; set; }

        public ulong? CreatorId { get; set; }

        public string CreatorName { get; set; }

        public string CreatorAvatarUrl { get; set; }

        public ulong? ItemDefinitionId { get; set; }

        public string ItemType { get; set; }

        public string ItemShortName { get; set; }

        public string ItemCollection { get; set; }

        public string Name { get; set; }

        public string NameHash { get; set; }

        public string NameWorkshop { get; set; }

        public ulong? NameId { get; set; }

        public string Description { get; set; }

        public string DescriptionWorkshop { get; set; }

        public string[] Notes { get; set; }

        public IDictionary<long, string> Changes { get; set; }

        public IDictionary<string, string> Tags { get; set; }

        public bool IsPermanent { get; set; }

        public bool? HasGlow { get; set; }

        public bool? HasGlowSights { get; set; }

        public decimal? GlowRatio { get; set; }

        public bool? HasCutout { get; set; }

        public decimal? CutoutRatio { get; set; }

        public string DominantColour { get; set; }

        public string BackgroundColour { get; set; }

        public string ForegroundColour { get; set; }

        public string IconUrl { get; set; }

        public string IconLargeUrl { get; set; }

        public string PreviewUrl { get; set; }

        public IDictionary<string, SteamMediaType> Previews { get; set; }

        public long? SupplyTotalEstimated { get; set; }

        public long? SupplyTotalMarketsKnown { get; set; }

        public long? SupplyTotalInvestorsKnown { get; set; }

        public long? SupplyTotalInvestorsEstimated { get; set; }

        public long? SupplyTotalOwnersKnown { get; set; }

        public long? SupplyTotalOwnersEstimated { get; set; }

        public long? SubscriptionsCurrent { get; set; }

        public long? SubscriptionsLifetime { get; set; }

        public long? FavouritedCurrent { get; set; }

        public long? FavouritedLifetime { get; set; }

        public long? Views { get; set; }

        public uint? VotesUp { get; set; }

        public uint? VotesDown { get; set; }

        public bool IsCommodity { get; set; }

        public bool IsMarketable { get; set; }

        public int? MarketableRestrictionDays { get; set; }

        public bool IsTradable { get; set; }

        public int? TradableRestrictionDays { get; set; }

        public bool IsSpecialDrop { get; set; }

        public bool IsTwitchDrop { get; set; }

        public bool IsCraftingComponent { get; set; }

        public bool IsCraftable { get; set; }

        public IDictionary<string, int> CraftingComponents { get; set; }

        public bool IsBreakable { get; set; }

        public IDictionary<string, int> BreaksIntoComponents { get; set; }

        public bool IsBanned { get; set; }

        public string BanReason { get; set; }

        public bool IsAccepted { get; set; }

        public DateTimeOffset? TimeAccepted { get; set; }

        public DateTimeOffset? TimeUpdated { get; set; }

        public DateTimeOffset? TimeCreated { get; set; }

        public DateTimeOffset? TimeRefreshed { get; set; }

        #endregion

        #region Store Item 

        public bool IsAvailableOnStore { get; set; }

        public bool HasReturnedToStoreBefore { get; set; }

        public long? StoreId { get; set; }

        public long? StorePrice { get; set; }

        public ItemStoreInstanceDTO[] Stores { get; set; }

        #endregion

        #region Market Item

        public bool IsAvailableOnMarket { get; set; }

        public string MarketId { get; set; }

        public int? MarketBuyOrderCount { get; set; }

        public long? MarketBuyOrderHighestPrice { get; set; }

        public int? MarketSellOrderCount { get; set; }

        public long? MarketSellOrderLowestPrice { get; set; }

        public long? Market1hrSales { get; set; }

        public long? Market1hrValue { get; set; }

        public long? Market24hrSales { get; set; }

        public long? Market24hrValue { get; set; }

        public long? MarketLastSaleValue { get; set; }

        public DateTimeOffset? MarketLastSaleOn { get; set; }

        public long? MarketHighestValue { get; set; }

        public long? MarketLowestValue { get; set; }

        public long? MarketRankIndex { get; set; }

        public long? MarketRankTotal { get; set; }

        public DateTimeOffset? TimeMarketHighestValue { get; set; }

        public DateTimeOffset? TimeMarketLowestValue { get; set; }

        public DateTimeOffset? TimeMarketFirstSold { get; set; }
        
        public bool IsBeingManipulated { get; set; }

        #endregion

        #region Prices

        public MarketType? BuyNowFrom { get; set; }

        public long? BuyNowPrice { get; set; }

        public string BuyNowUrl { get; set; }

        public ItemMarketPriceDTO[] BuyPrices { get; set; }

        // TODO: BuyLater
        // TODO: SellNow
        // TODO: SellLater

        #endregion

        #region Interactions

        public ItemInteractionDTO[] Actions { get; set; }

        #endregion
    }
}
