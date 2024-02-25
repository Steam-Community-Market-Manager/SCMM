using System.ComponentModel.DataAnnotations;

namespace SCMM.Steam.Data.Models.Enums
{
    public enum SteamMarketItemActivityType
    {
        [Display(Name = "Other")]
        Other = 0,

        [Display(Name = "Sell Order Created")]
        CreatedSellOrder,

        [Display(Name = "Sell Order Cancelled")]
        CancelledSellOrder,

        [Display(Name = "Buy Order Created")]
        CreatedBuyOrder,

        [Display(Name = "Buy Order Cancelled")]
        CancelledBuyOrder
    }
}
