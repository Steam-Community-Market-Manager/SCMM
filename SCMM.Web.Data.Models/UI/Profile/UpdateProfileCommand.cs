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

        public PriceFlags PaymentTypes { get; set; }

        public MarketType[] MarketTypes { get; set; }

        public MarketValueType MarketValue { get; set; }

        public ItemInfoType[] ItemInfo { get; set; }

        public ItemInfoWebsiteType ItemInfoWebsite { get; set; }

        public bool ItemIncludeMarketFees { get; set; }

        public bool InventoryShowItemDrops { get; set; }

        public bool InventoryShowUnmarketableItems { get; set; }

        public InventoryValueMovementDisplayType InventoryValueMovemenDisplay { get; set; }

        public Dictionary<string, string[]> Notifications { get; set; }

        public ProfileWebhook[] Webhooks { get; set; }
    }

    public class ProfileWebhook
    {
        public string Id { get; set; }

        public string Endpoint { get; set; }

        public string Secret { get; set; }
    }
}
