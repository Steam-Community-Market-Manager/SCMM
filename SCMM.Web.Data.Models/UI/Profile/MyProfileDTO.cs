using SCMM.Steam.Data.Models.Enums;
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

        public StoreTopSellerRankingType StoreTopSellers { get; set; }

        public MarketValueType MarketValue { get; set; }

        public bool IncludeMarketTax { get; set; }

        public IEnumerable<ItemInfoType> ItemInfo { get; set; }

        public ItemInfoWebsiteType ItemInfoWebsite { get; set; }

        public bool ShowItemDrops { get; set; }

        public string DiscordId { get; set; }

        public string[] Roles { get; set; }

        public int DonatorLevel { get; set; }
    }
}
