using System.Text.Json.Serialization;

namespace SCMM.Web.Data.Models.UI.Store
{
    public class StoreItemDetailsDTO : IItemDescription
    {
        public Guid Guid { get; set; }

        public ulong Id { get; set; }

        public ulong AppId { get; set; }

        public ulong? AssetDescriptionId { get; set; }

        public ulong? WorkshopFileId { get; set; }

        public string WorkshopFileUrl { get; set; }

        [JsonIgnore]
        public bool HasWorkshopFile => (WorkshopFileId != null);

        public string MarketListingId { get; set; }

        [JsonIgnore]
        public bool HasMarketListing => !string.IsNullOrEmpty(MarketListingId);

        public ulong? CreatorId { get; set; }

        public string CreatorName { get; set; }

        public string CreatorAvatarUrl { get; set; }

        public string ItemType { get; set; }

        public string ItemCollection { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool IsPermanent { get; set; }

        public bool? HasGlow { get; set; }

        public bool? HasGlowSights { get; set; }

        public decimal? GlowRatio { get; set; }

        public bool? HasCutout { get; set; }

        public decimal? CutoutRatio { get; set; }

        public string BackgroundColour { get; set; }

        public string ForegroundColour { get; set; }

        public string IconAccentColour { get; set; }

        public string IconUrl { get; set; }

        public DateTimeOffset? TimeCreated { get; set; }

        public DateTimeOffset? TimeAccepted { get; set; }

        public long? StorePrice { get; set; }

        public long? StorePriceUsd { get; set; }

        public int? TopSellerIndex { get; set; }

        public bool IsStillAvailableFromStore { get; set; }

        public bool HasReturnedToStoreBefore { get; set; }

        public long? MarketPrice { get; set; }

        public long? MarketSupply { get; set; }

        public long? MarketDemand24hrs { get; set; }

        public int MarketRankIndex { get; set; }

        public int MarketRankTotal { get; set; }

        public long? SupplyTotalEstimated { get; set; }

        public long? Subscriptions { get; set; }

        public bool IsMarketable { get; set; }

        public int? MarketableRestrictionDays { get; set; }

        public bool IsTradable { get; set; }

        public int? TradableRestrictionDays { get; set; }

        public bool IsSpecialDrop { get; set; }

        public bool IsBreakable { get; set; }

        public IDictionary<string, uint> BreaksIntoComponents { get; set; }

        public bool IsBanned { get; set; }

        public string BanReason { get; set; }

        public string[] Notes { get; set; }

        public bool IsDraft { get; set; }
    }
}
