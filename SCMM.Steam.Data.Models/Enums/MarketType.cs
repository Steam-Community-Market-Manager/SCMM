using SCMM.Steam.Data.Models.Attributes;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Steam.Data.Models.Enums
{
    public enum MarketType : byte
    {
        [Display(Name = "Unknown")]
        Unknown = 0,

        [Display(Name = "Steam Store")]
        [Market(Type = PriceTypes.Cash)]
        [BuyFrom(Url = "https://store.steampowered.com/itemstore/{0}/")]
        SteamStore,

        [Display(Name = "Steam Community Market")]
        [Market(Type = PriceTypes.Cash)]
        [BuyFrom(Url = "https://steamcommunity.com/market/listings/{0}/{3}")]
        [SellTo(Url = "https://steamcommunity.com/market/listings/{0}/{3}", FeeRate = 13f)]
        SteamCommunityMarket,

        [Display(Name = "Skinport")]
        [Market(Type = PriceTypes.Cash)]
        [BuyFrom(Url = "https://skinport.com/{1}/market?r=scmm&item={3}")]
        Skinport,

        [Display(Name = "LOOT.Farm")]
        [Market(Type = PriceTypes.Cash | PriceTypes.Trade)]
        [BuyFrom(Url = "https://loot.farm/", FeeRate = -20f)] // Unconfirmed, first time only?
        LOOTFarm,

        [Display(Name = "Swap.gg Trade")]
        [Market(Type = PriceTypes.Trade)]
        [BuyFrom(Url = "https://swap.gg?idev_id=326&appId={0}&search={3}")]
        SwapGGTrade,

        [Display(Name = "Swap.gg Market")]
        [Market(Type = PriceTypes.Cash)]
        [BuyFrom(Url = "https://market.swap.gg/browse?idev_id=326&appId={0}&search={3}", FeeSurcharge = 40, FeeRate = 3f)] /* 0,33 EUR + 3% */
        SwapGGMarket,

        [Display(Name = "Tradeit.gg Trade")]
        [Market(Type = PriceTypes.Trade)]
        [BuyFrom(Url = "https://tradeit.gg/{1}/trade?aff=scmm&search={3}")]
        TradeitGGTrade,

        [Display(Name = "Tradeit.gg Store")]
        [Market(Type = PriceTypes.Cash)]
        [BuyFrom(Url = "https://tradeit.gg/{1}/store?aff=scmm&search={3}")] // Unconfirmed
        TradeitGGStore,

        [Display(Name = "CS.Deals Trade")]
        [Market(Type = PriceTypes.Trade)]
        [BuyFrom(Url = "https://cs.deals/trade-skins")]
        CSDealsTrade,

        [Display(Name = "CS.Deals Marketplace")]
        [Market(Type = PriceTypes.Cash)]
        [BuyFrom(Url = "https://cs.deals/market/{1}/?name={3}&sort=price")] // Unconfirmed
        CSDealsMarketplace,

        [Display(Name = "SkinBaron")]
        [Market(Type = PriceTypes.Cash)]
        [BuyFrom(Url = "https://skinbaron.de/en/{1}?str={3}&sort=CF", FeeSurcharge = 40, FeeRate = 5f)] // Unconfirmed
        SkinBaron,

        [Display(Name = "RUSTSkins")]
        [Market(Type = PriceTypes.Cash)]
        [BuyFrom(Url = "https://rustskins.com/market?search={3}&sort=p-ascending")] // Unconfirmed
        RUSTSkins,

        /*
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
