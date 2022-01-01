using SCMM.Steam.Data.Models.Attributes;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Steam.Data.Models.Enums
{
    public enum PriceType : byte
    {
        [Display(Name = "Unknown")]
        Unknown = 0,

        [Display(Name = "Steam Store")]
        [BuyFrom(Url = "https://store.steampowered.com/itemstore/{0}/")]
        SteamStore,

        [Display(Name = "Steam Community Market")]
        [BuyFrom(Url = "https://steamcommunity.com/market/listings/{0}/{3}")]
        [SalesTax(13)]
        SteamCommunityMarket,

        [Display(Name = "Skinport")]
        [BuyFrom(Url = "https://skinport.com/{1}/market?item={3}")]
        [SalesTax(13)]
        Skinport,

        [Display(Name = "swap.gg")]
        [BuyFrom(Url = "https://market.swap.gg/browse/{0}?search={3}")]
        [SalesTax(8)]
        SwapGG,

        [Display(Name = "Tradeit.gg")]
        [BuyFrom(Url = "https://tradeit.gg/{1}/store?search={3}")]
        [SalesTax(13)]
        TradeitGG,

        [Display(Name = "Dmarket")]
        [BuyFrom(Url = "https://dmarket.com/ingame-items/item-list/{1}-skins?title={3}")]
        [SalesTax(7)]
        Dmarket,

        [Display(Name = "CS.Deals")]
        [BuyFrom(Url = "https://cs.deals/market/{1}/?name={3}&sort=price")]
        [SalesTax(2)]
        CSDeals
    }
}
