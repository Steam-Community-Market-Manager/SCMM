using SCMM.Steam.Data.Models.Enums;

namespace SCMM.Web.Data.Models.UI.Profile
{
    public class UpdateProfileCommand
    {
        public string TradeUrl { get; set; }

        public ItemAnalyticsParticipationType ItemAnalyticsParticipation { get; set; }

        public long GamblingOffset { get; set; }

        public string Language { get; set; }

        public string Currency { get; set; }

        public StoreTopSellerRankingType StoreTopSellers { get; set; }

        public MarketValueType MarketValue { get; set; }

        public bool IncludeMarketTax { get; set; }

        public IEnumerable<ItemInfoType> ItemInfo { get; set; }

        public ItemInfoWebsiteType ItemInfoWebsite { get; set; }

        public bool ShowItemDrops { get; set; }

        public Dictionary<string, HashSet<string>> Notifications { get; set; }

        public string DiscordId { get; set; }

        public List<ProfileWebhook> Webhooks { get; set; }
    }

    public class ProfileWebhook
    {
        public string Id { get; set; }

        public string Endpoint { get; set; }

        public string Secret { get; set; }
    }
}
