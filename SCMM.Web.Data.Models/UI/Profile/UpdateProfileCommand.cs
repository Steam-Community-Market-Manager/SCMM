using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Data.Models.UI.Profile
{
    public class UpdateProfileCommand
    {
        public string TradeUrl { get; set; }

        public ItemAnalyticsParticipationType ItemAnalytics { get; set; }

        public long GamblingOffset { get; set; }

        public string Language { get; set; }

        public string Currency { get; set; }

        public StoreTopSellerRankingType StoreTopSellers { get; set; }

        public MarketValueType MarketValue { get; set; }

        public bool IncludeMarketTax { get; set; }

        public HashSet<ItemInfoType> ItemInfo { get; set; }

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

    public enum ItemAnalyticsParticipationType
    {
        [Display(Name = "Exclude me from item analytics")]
        Private = -1,

        [Display(Name = "Participate anonymously")]
        Anonymous = 0,

        [Display(Name = "Participate publically")]
        Public = 1
    }

    public enum StoreTopSellerRankingType
    {
        [Display(Name = "Highest Recent Revenue")]
        HighestRecentRevenue = 0,

        [Display(Name = "Highest Total Revenue")]
        HighestTotalRevenue,

        [Display(Name = "Highest Total Sales")]
        HighestTotalSales
    }

    public enum MarketValueType
    {
        [Display(Name = "Sell Order Prices")]
        SellOrderPrices = 0,

        [Display(Name = "Buy Order Prices")]
        BuyOrderPrices,

        [Display(Name = "Median Sale Prices")]
        MedianSalePrices
    }

    [Flags]
    public enum ItemInfoType
    {
        [Display(Name = "Supply")]
        Supply = 0x01,

        [Display(Name = "Demand")]
        Demand = 0x02,

        [Display(Name = "Subscriptions")]
        Subscriptions = 0x04
    }

    public enum ItemInfoWebsiteType
    {
        [Display(Name = "View in SCMM")]
        Internal = 0,

        [Display(Name = "View on Steam")]
        Steam
    }
}
