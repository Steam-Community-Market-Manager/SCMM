using SCMM.Steam.Data.Models.Enums;
using SCMM.Web.Data.Models.UI.App;
using SCMM.Web.Data.Models.UI.Currency;
using SCMM.Web.Data.Models.UI.Language;

namespace SCMM.Web.Data.Models.UI.Profile
{
    public class MyProfileDTO : ProfileDetailedDTO
    {
        public string AvatarLargeUrl { get; set; }

        public string TradeUrl { get; set; }

        public ItemAnalyticsParticipationType ItemAnalyticsParticipation { get; set; }

        public long GamblingOffset { get; set; }

        public LanguageDetailedDTO Language { get; set; }

        public CurrencyDetailedDTO Currency { get; set; }

        public AppDetailedDTO App { get; set; }

        public StoreTopSellerRankingType StoreTopSellers { get; set; }

        public PriceFlags PaymentTypes { get; set; }

        public MarketType[] MarketTypes { get; set; }

        public MarketValueType MarketValue { get; set; }

        public ItemInfoType[] ItemInfo { get; set; }

        public ItemInfoWebsiteType ItemInfoWebsite { get; set; }

        public bool ItemIncludeMarketFees { get; set; }

        public bool InventoryShowItemDrops { get; set; }

        public bool InventoryShowUnmarketableItems { get; set; }

        public InventoryValueMovementDisplayType InventoryValueMovementDisplay { get; set; }

        public int DonatorLevel { get; set; }
    }
}
