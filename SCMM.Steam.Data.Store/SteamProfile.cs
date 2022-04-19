using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store;
using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.Data.Models.Enums;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Store
{
    public class SteamProfile : Entity
    {
        public SteamProfile()
        {
            Preferences = new PersistableStringDictionary();
            Roles = new PersistableStringCollection();
            InventoryItems = new Collection<SteamProfileInventoryItem>();
            MarketItems = new Collection<SteamProfileMarketItem>();
            AssetDescriptions = new Collection<SteamAssetDescription>();
        }

        public string SteamId { get; set; }

        public string ProfileId { get; set; }

        public string DiscordId { get; set; }

        public string Name { get; set; }

        public string AvatarUrl { get; set; }

        public string AvatarLargeUrl { get; set; }

        public string TradeUrl { get; set; }

        public Guid? LanguageId { get; set; }

        public SteamLanguage Language { get; set; }

        public Guid? CurrencyId { get; set; }

        public SteamCurrency Currency { get; set; }

        public int LastTotalInventoryItems { get; set; }

        public long LastTotalInventoryValue { get; set; }

        public DateTimeOffset? LastViewedInventoryOn { get; set; }

        public DateTimeOffset? LastUpdatedInventoryOn { get; set; }

        public DateTimeOffset? LastSignedInOn { get; set; }

        public int DonatorLevel { get; set; }

        public long GamblingOffset { get; set; }

        public SteamVisibilityType Privacy { get; set; } = SteamVisibilityType.Unknown;

        public ItemAnalyticsParticipationType ItemAnalyticsParticipation { get; set; } = ItemAnalyticsParticipationType.Public;

        [Required]
        public PersistableStringDictionary Preferences { get; set; }

        [JsonIgnore]
        [NotMapped]
        public StoreTopSellerRankingType StoreTopSellers
        {
            get { return Enum.Parse<StoreTopSellerRankingType>(Preferences.GetOrDefault(nameof(StoreTopSellers), StoreTopSellerRankingType.HighestTotalSales.ToString())); }
            set { Preferences[nameof(StoreTopSellers)] = value.ToString(); }
        }

        [JsonIgnore]
        [NotMapped]
        public MarketValueType MarketValue
        {
            get { return Enum.Parse<MarketValueType>(Preferences.GetOrDefault(nameof(MarketValue), MarketValueType.SellOrderPrices.ToString())); }
            set { Preferences[nameof(MarketValue)] = value.ToString(); }
        }

        [JsonIgnore]
        [NotMapped]
        public IEnumerable<ItemInfoType> ItemInfo
        {
            get { return Enum.GetValues<ItemInfoType>().Where(x => !Preferences.ContainsKey(nameof(ItemInfo)) || Preferences[nameof(ItemInfo)].Contains(x.ToString())); }
            set { Preferences[nameof(ItemInfo)] = value.Aggregate((ItemInfoType)0, (a, b) => a |= b).ToString(); }
        }

        [JsonIgnore]
        [NotMapped]
        public ItemInfoWebsiteType ItemInfoWebsite
        {
            get { return Enum.Parse<ItemInfoWebsiteType>(Preferences.GetOrDefault(nameof(ItemInfoWebsite), ItemInfoWebsiteType.External.ToString())); }
            set { Preferences[nameof(ItemInfoWebsite)] = value.ToString(); }
        }

        [JsonIgnore]
        [NotMapped]
        public bool ItemIncludeMarketFees
        {
            get { return bool.Parse(Preferences.GetOrDefault(nameof(ItemIncludeMarketFees), Boolean.TrueString)); }
            set { Preferences[nameof(ItemIncludeMarketFees)] = value.ToString(); }
        }

        [JsonIgnore]
        [NotMapped]
        public bool InventoryShowItemDrops
        {
            get { return bool.Parse(Preferences.GetOrDefault(nameof(InventoryShowItemDrops), Boolean.FalseString)); }
            set { Preferences[nameof(InventoryShowItemDrops)] = value.ToString(); }
        }

        [JsonIgnore]
        [NotMapped]
        public bool InventoryShowUnmarketableItems
        {
            get { return bool.Parse(Preferences.GetOrDefault(nameof(InventoryShowUnmarketableItems), Boolean.FalseString)); }
            set { Preferences[nameof(InventoryShowUnmarketableItems)] = value.ToString(); }
        }

        [JsonIgnore]
        [NotMapped]
        public InventoryValueMovementDisplayType InventoryValueMovementDisplay
        {
            get { return Enum.Parse<InventoryValueMovementDisplayType>(Preferences.GetOrDefault(nameof(InventoryValueMovementDisplay), InventoryValueMovementDisplayType.Price.ToString())); }
            set { Preferences[nameof(InventoryValueMovementDisplay)] = value.ToString(); }
        }

        [Required]
        public PersistableStringCollection Roles { get; set; }

        public ICollection<SteamProfileInventoryItem> InventoryItems { get; set; }

        public ICollection<SteamProfileMarketItem> MarketItems { get; set; }

        public ICollection<SteamAssetDescription> AssetDescriptions { get; set; }

        public void RemoveNonEssentialData()
        {
            ProfileId = null;
            DiscordId = null;
            ProfileId = null;
            TradeUrl = null;
            LanguageId = null;
            CurrencyId = null;
            LastTotalInventoryValue = 0;
            LastTotalInventoryItems = 0;
            LastViewedInventoryOn = null;
            LastUpdatedInventoryOn = null;
            LastSignedInOn = null;
            DonatorLevel = 0;
            GamblingOffset = 0;
            Privacy = SteamVisibilityType.Unknown;
            ItemAnalyticsParticipation = ItemAnalyticsParticipationType.Anonymous;
            Preferences?.Clear();
            Roles?.Clear();
            InventoryItems?.Clear();
            MarketItems?.Clear();
        }
    }
}
