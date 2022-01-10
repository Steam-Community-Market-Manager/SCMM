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
        [SellTo(Url = "https://steamcommunity.com/market/listings/{0}/{3}", FeeRate = 13.0f)]
        SteamCommunityMarket,

        [Display(Name = "Skinport")]
        [BuyFrom(Url = "https://skinport.com/{1}/market?r=scmm&item={3}", FeeRate = 7.0f)] // Unconfirmed
        Skinport,

        [Display(Name = "LOOT.Farm")]
        [BuyFrom(Url = "https://loot.farm/", FeeRate = -20.0f)] // Unconfirmed, first time only?
        LOOTFarm,

        [Display(Name = "swap.gg Trade")]
        [BuyFrom(Url = "https://swap.gg?idev_id=326&appId={0}&search={3}")]
        SwapGGTrade,

        [Display(Name = "swap.gg Market")]
        [BuyFrom(Url = "https://market.swap.gg/browse?idev_id=326&appId={0}&search={3}", FeeRate = 3.0f, FeeSurcharge = 40)] // Unconfirmed
        SwapGGMarket,

        [Display(Name = "Tradeit.gg Trade")]
        [BuyFrom(Url = "https://tradeit.gg/{1}/trade?aff=scmm&search={3}")]
        TradeitGGTrade,

        [Display(Name = "Tradeit.gg Store")]
        [BuyFrom(Url = "https://tradeit.gg/{1}/store?aff=scmm&search={3}")] // Unconfirmed
        TradeitGGStore,

        [Display(Name = "CS.Deals Trade")]
        [BuyFrom(Url = "https://cs.deals/trade-skins")] // Unconfirmed
        CSDealsTrade,

        [Display(Name = "CS.Deals Marketplace")]
        [BuyFrom(Url = "https://cs.deals/market/{1}/?name={3}&sort=price")] // Unconfirmed
        CSDealsMarketplace,

        /*
        [Display(Name = "SkinBaron")]
        //[BuyFrom(Url = "https://skinbaron.de/en/{1}?str={3}&sort=CF", FeeRate = 5.0f, FeeSurcharge = 40)]
        // https://skinbaron.de/misc/apidoc/
        SkinBaron,

        [Display(Name = "RUSTSkins")]
        //[BuyFrom(Url = "https://rustskins.com/market?search={3}&sort=p-ascending")]
        RUSTSkins,

        [Display(Name = "Dmarket")]
        //[BuyFrom(Url = "https://dmarket.com/ingame-items/item-list/{1}-skins?title={3}")]
        // https://docs.dmarket.com/v1/swagger.html#/Buy%20items/GetAggregatedPrices
        // https://api.dmarket.com/price-aggregator/v1/aggregated-prices?Titles=Blackout%20Hoodie&Titles=Blackout%20Pants&Limit=100
        Dmarket,

        [Display(Name = "BUFF")]
        //[BuyFrom(Url = "https://buff.163.com/market/{1}")]
        Buff

        // TODO: https://skinmarket.gg/
        */
    }
}
