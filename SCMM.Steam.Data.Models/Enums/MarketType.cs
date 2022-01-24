using SCMM.Steam.Data.Models.Attributes;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Steam.Data.Models.Enums
{
    public enum MarketType : byte
    {
        [Display(Name = "Unknown")]
        Unknown = 0,

        [Display(Name = "Steam Store")]
        [Market(Type = PriceTypes.Cash, Color = "#171A21")]
        [BuyFrom(Url = "https://store.steampowered.com/itemstore/{0}/")]
        SteamStore = 1,

        [Display(Name = "Steam Community Market")]
        [Market(Type = PriceTypes.Cash | PriceTypes.Trade, Color = "#171A21")]
        [BuyFrom(Url = "https://steamcommunity.com/market/listings/{0}/{3}")]
        [SellTo(Url = "https://steamcommunity.com/market/listings/{0}/{3}", FeeRate = 13f)]
        SteamCommunityMarket = 2,

        [Display(Name = "Skinport")]
        [Market(Type = PriceTypes.Cash, Color = "#232728")]
        [BuyFrom(Url = "https://skinport.com/{1}/market?r=scmm&item={3}")]
        Skinport = 10,

        [Display(Name = "LOOT.Farm")]
        [Market(Type = PriceTypes.Cash | PriceTypes.Trade, Color = "#123E64")]
        [BuyFrom(Url = "https://loot.farm/", FeeRate = -17f)] /* 20% bonus balance - 3% card fee */
        LOOTFarm = 11,

        [Display(Name = "Swap.gg")]
        [Market(Type = PriceTypes.Trade, Color = "#15C7AD")]
        [BuyFrom(Url = "https://swap.gg?idev_id=326&appId={0}&search={3}")]
        SwapGGTrade = 12,

        [Display(Name = "Swap.gg Market")]
        [Market(Type = PriceTypes.Cash, Color = "#15C7AD")]
        [BuyFrom(Url = "https://market.swap.gg/browse?idev_id=326&appId={0}&search={3}", FeeSurcharge = 40, FeeRate = 3f)] /* Roughly 0,33 EUR + 3% card fee */
        SwapGGMarket = 13,

        [Display(Name = "Tradeit.gg")]
        [Market(Type = PriceTypes.Cash | PriceTypes.Trade, Color = "#27273F")]
        [BuyFrom(Url = "https://tradeit.gg/{1}/store?aff=scmm&search={3}", FeeRate = -25f)] /* 25% bonus balance */
        TradeitGG = 14,

        [Display(Name = "CS.Deals Trade")]
        [Market(Type = PriceTypes.Trade, Color = "#313846")]
        [BuyFrom(Url = "https://cs.deals/trade-skins")]
        CSDealsTrade = 15,

        // TODO: Add missing items quantities
        [Display(Name = "CS.Deals Marketplace")]
        [Market(Type = PriceTypes.Cash, Color = "#313846")]
        [BuyFrom(Url = "https://cs.deals/market/{1}/?name={3}&sort=price", FeeSurcharge = 45, FeeRate = 3f)] /* Roughly 0,40 EUR + 3% card fee */
        CSDealsMarketplace = 16,

        [Display(Name = "Skin Baron")]
        [Market(Type = PriceTypes.Cash, Color = "#2A2745")]
        [BuyFrom(Url = "https://skinbaron.de/en/{1}?str={3}&sort=CF", FeeSurcharge = 40, FeeRate = 5f)] /* Roughly 0,33 EUR + 5% card fee */ // Unconfirmed
        SkinBaron = 17,

        [Display(Name = "RUST Skins")]
        [Market(Type = PriceTypes.Cash, Color = "#EF7070")]
        [BuyFrom(Url = "https://rustskins.com/market?search={3}&sort=p-ascending", FeeRate = 3f)]
        RUSTSkins = 18,

        [Display(Name = "Rust.tm")]
        [Market(Type = PriceTypes.Cash)]
        [BuyFrom(Url = "https://rust.tm/?s=price&t=all&search={3}&sd=asc")] // Unconfirmed
        RustTM = 19,

        // TODO: Implement web socket client support
        // wss://rustvendor.com/socket.io/?EIO=4&transport=websocket&sid=xxx
        // => 42["requestInventory"]
        // <= 42["requestInventoryResponse",…]
        [Display(Name = "RUSTVendor")]
        [Market(Type = PriceTypes.Cash | PriceTypes.Trade)] // +50% crypto deposits
        [BuyFrom(Url = "https://rustvendor.com/trade")] // Unconfirmed
        RUSTVendor = 20,

        // TODO: Implement web socket client support
        // wss://rustytrade.com/socket.io/?EIO=3&transport=websocket&sid=xxx
        // => 42["get bots inv"]
        // <= 42["bots inv",…]
        [Display(Name = "RustyTrade")]
        [Market(Type = PriceTypes.Trade)]
        [BuyFrom(Url = "https://rustytrade.com/")] // Unconfirmed
        RustyTrade = 21,

        [Display(Name = "CS.TRADE")]
        [Market(Type = PriceTypes.Trade)]
        [BuyFrom(Url = "https://cs.trade/#trader")] // Unconfirmed
        CSTRADE = 22,

        [Display(Name = "iTrade.gg")]
        [Market(Type = PriceTypes.Trade)]
        [BuyFrom(Url = "https://itrade.gg/?userInv={1}&botInv={1}")] // Unconfirmed
        iTradegg = 23,

        [Display(Name = "Trade Skins Fast")]
        [Market(Type = PriceTypes.Trade)]
        [BuyFrom(Url = "https://tradeskinsfast.com/")] // Unconfirmed
        TradeSkinsFast = 24,

        [Display(Name = "SkinsMonkey")]
        [Market(Type = PriceTypes.Cash | PriceTypes.Trade)] // +25% cash deposits
        [BuyFrom(Url = "https://skinsmonkey.com/trade-{1}-skins")] // Unconfirmed
        SkinsMonkey = 25,

        [Display(Name = "skinmarket.gg")]
        [Market(Type = PriceTypes.Cash | PriceTypes.Trade)] // +40% crypto deposits
        [BuyFrom(Url = "https://skinmarket.gg/")] // Unconfirmed
        skinmarketGG = 26,

        // TODO: Find workaround for overly agressive CloudFlare policies
        [Display(Name = "GAMERALL.com")]
        [Market(Type = PriceTypes.Cash)]
        [BuyFrom(Url = "https://gamerall.com/rust")] // Unconfirmed
        GAMERALL = 27,

        // TODO: F2F market support
        [Display(Name = "Dmarket")]
        [Market(Type = PriceTypes.Cash | PriceTypes.Trade, Color = "#49BC74")]
        [BuyFrom(Url = "https://dmarket.com/ingame-items/item-list/{1}-skins?ref=6tlej6xqvD&title={3}")] // Unconfirmed
        Dmarket = 28,

        // TODO: Login support
        [Display(Name = "BUFF")]
        [Market(Type = PriceTypes.Cash, Color = "#FFFFFF")]
        [BuyFrom(Url = "https://buff.163.com/market/{1}#tab=selling&sort_by=price.asc&search={3}")] // Unconfirmed
        Buff = 29

        /*
        BUY:  https://trade.skin/ (looks sus...)
        BUY:  https://rustplus.com/ (looks sus...)
        SELL: https://rustysell.com/
        SELL: https://skincashier.com/
        SELL: https://skins.cash/
        */
    }
}
