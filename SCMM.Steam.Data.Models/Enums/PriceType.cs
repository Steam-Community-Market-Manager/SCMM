using System.ComponentModel.DataAnnotations;

namespace SCMM.Steam.Data.Models.Enums
{
    public enum PriceType : byte
    {
        [Display(Name = "Unknown")]
        Unknown = 0,

        [Display(Name = "Steam Store")]
        SteamStore,

        [Display(Name = "Steam Community Market")]
        SteamCommunityMarket,

        [Display(Name = "Skinport")]
        Skinport,

        [Display(Name = "BitSkins")]
        BitSkins,

        [Display(Name = "swap.gg")]
        SwapGG,

        [Display(Name = "Tradeit.gg")]
        TradeitGG,

        [Display(Name = "Dmarket")]
        Dmarket,

        [Display(Name = "CS.Deals")]
        CSDeals,
    }
}
