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
    [Display(Name = "Use Steam Store Ranking")]
    SteamStoreRanking = 0,

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
    [Display(Name = "Original Price")]
    StorePrice = 0x01,

    [Display(Name = "Supply")]
    Supply = 0x02,

    [Display(Name = "Demand")]
    Demand = 0x04,

    [Display(Name = "Subscriptions")]
    Subscriptions = 0x08
}

public enum ItemInfoWebsiteType
{
    [Display(Name = "SCMM")]
    Internal = 0,

    [Display(Name = "Steam")]
    External
}
