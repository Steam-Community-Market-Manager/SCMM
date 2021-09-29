using System.ComponentModel.DataAnnotations;

namespace SCMM.Steam.Data.Models.Enums;

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
