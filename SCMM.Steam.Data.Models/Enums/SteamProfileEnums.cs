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
    [Display(Name = "Lowest Sell Order Price")]
    SellOrderPrices = 0,

    [Display(Name = "Highest Buy Order Price")]
    BuyOrderPrices,

    [Display(Name = "Median 24hr Sale Price")]
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

    [Display(Name = "Subscribers")]
    Subscriptions = 0x08,

    [Display(Name = "Age")]
    Age = 0x10,

    [Display(Name = "Estimated Total Supply")]
    EstimatedTotalSupply = 0x20,
}

public enum ItemInfoWebsiteType
{
    [Display(Name = "SCMM")]
    Internal = 0,

    [Display(Name = "Steam")]
    External
}

public enum InventoryValueMovementDisplayType
{
    [Display(Name = "Show as price")]
    Price = 0,

    [Display(Name = "Show as percentage")]
    Percentage
}
