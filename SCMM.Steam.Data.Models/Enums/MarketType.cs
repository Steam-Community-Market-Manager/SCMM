using SCMM.Steam.Data.Models.Attributes;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Steam.Data.Models.Enums
{
    /// <summary>
    /// All known game item markets
    /// </summary>
    public enum MarketType : byte
    {
        [Display(Name = "Unknown")]
        Unknown = 0,

        [Display(Name = "Steam Store")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Constants.UnturnedAppId, IsFirstParty = true, Color = "#171A21")]
        [BuyFrom(Url = "https://store.steampowered.com/itemstore/{0}/", AcceptedPaymentTypes = PriceTypes.Cash)]
        SteamStore = 1,

        [Display(Name = "Steam Community Market")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Constants.UnturnedAppId, IsFirstParty = true, Color = "#171A21")]
        [BuyFrom(Url = "https://steamcommunity.com/market/listings/{0}/{3}", AcceptedPaymentTypes = PriceTypes.Cash)]
        [SellTo(Url = "https://steamcommunity.com/market/listings/{0}/{3}", AcceptedPaymentTypes = PriceTypes.Cash, FeeRate = 13.0f)]
        SteamCommunityMarket = 2,

        [Display(Name = "Skinport")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#FA490A", AffiliateUrl = "https://skinport.com/r/scmm")]
        [BuyFrom(Url = "https://skinport.com/{1}/market?r=scmm&item={3}", AcceptedPaymentTypes = PriceTypes.Cash)]
        Skinport = 10,

        [Display(Name = "LOOT.Farm")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#06FDDE")]
        [BuyFrom(Url = "https://loot.farm/", AcceptedPaymentTypes = PriceTypes.Trade | PriceTypes.Cash | PriceTypes.Crypto)]
        LOOTFarm = 11,

        [Display(Name = "Swap.gg")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#15C9AF", AffiliateUrl = "https://swap.gg/?r=xu9CNezP5w")]
        [BuyFrom(Url = "https://swap.gg/?r=xu9CNezP5w&game={0}", AcceptedPaymentTypes = PriceTypes.Trade | PriceTypes.Cash)]
        SwapGGTrade = 12,

        // TODO: [Obsolete("Website discontinued from 29th February 2024")]
        [Display(Name = "Swap.gg - Market")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#15C9AF")]
        [BuyFrom(Url = "https://market.swap.gg/{1}?search={3}", AcceptedPaymentTypes = PriceTypes.Cash)]
        SwapGGMarket = 13,

        [Display(Name = "Tradeit.gg")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#27273F", AffiliateUrl = "https://tradeit.gg/?aff=scmm")]
        [BuyFrom(Url = "https://tradeit.gg/{1}/trade?aff=scmm&search={3}", AcceptedPaymentTypes = PriceTypes.Trade | PriceTypes.Cash | PriceTypes.Crypto)]
        [BuyFrom(Url = "https://tradeit.gg/{1}/store?aff=scmm&search={3}", AcceptedPaymentTypes = PriceTypes.Cash | PriceTypes.Crypto, DiscountMultiplier = 0.25f /* 25% */)]
        TradeitGG = 14,

        [Display(Name = "CS.Deals - Trade")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#78FFFF")]
        [BuyFrom(Url = "https://cs.deals/trade-skins?appid={0}", AcceptedPaymentTypes = PriceTypes.Trade | PriceTypes.Cash | PriceTypes.Crypto)]
        CSDealsTrade = 15,

        // TODO: Items quantities are not currently supported
        [Display(Name = "CS.Deals - Marketplace")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#78FFFF")]
        [BuyFrom(Url = "https://cs.deals/market/{1}/?name={3}&sort=price", AcceptedPaymentTypes = PriceTypes.Cash | PriceTypes.Crypto)]
        CSDealsMarketplace = 16,

        // TODO: Our web client needs updating, the current APIs no longer work (they've changed them)
        [Obsolete("SkinBaron web client code needs updating, current APIs no longer work (they've changed them)")]
        [Display(Name = "Skin Baron")]
        [Market(Constants.CSGOAppId, Color = "#2A2745")]
        [BuyFrom(Url = "https://skinbaron.de/en/{1}?str={3}&sort=CF", AcceptedPaymentTypes = PriceTypes.Cash)]
        SkinBaron = 17,

        [Obsolete("Needs to be revalidated again. Website is back online, but social links are dead")]
        [Display(Name = "RUST Skins")]
        [Market(Constants.RustAppId, Color = "#EF7070")]
        [BuyFrom(Url = "https://rustskins.com/market?search={3}&sort=p-ascending", AcceptedPaymentTypes = PriceTypes.Cash)]
        RUSTSkins = 18,

        [Display(Name = "Rust.tm")]
        [Market(Constants.RustAppId, Color = "#4E2918")]
        [BuyFrom(Url = "https://rust.tm/?s=price&t=all&search={3}&sd=asc", AcceptedPaymentTypes = PriceTypes.Cash | PriceTypes.Crypto)]
        RustTM = 19,

        [Obsolete("Website is dead")]
        [Display(Name = "RUSTVendor")]
        [Market(Constants.RustAppId)]
        [BuyFrom(Url = "https://rustvendor.com/trade", AcceptedPaymentTypes = PriceTypes.Trade | PriceTypes.Cash)]
        RUSTVendor = 20,

        [Obsolete("Website is live, but seems to be inactive now")]
        [Display(Name = "RustyTrade")]
        [Market(Constants.RustAppId)]
        [BuyFrom(Url = "https://rustytrade.com/", AcceptedPaymentTypes = PriceTypes.Trade)]
        RustyTrade = 21,

        [Display(Name = "CS.TRADE")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#F3C207", AffiliateUrl = "https://cs.trade/ref/SCMM")]
        [BuyFrom(Url = "https://cs.trade/ref/SCMM#trader", AcceptedPaymentTypes = PriceTypes.Trade | PriceTypes.Cash | PriceTypes.Crypto)]
        CSTRADE = 22,

        [Display(Name = "iTrade.gg")]
        [Market(Constants.RustAppId, Color = "#EA473B", AffiliateUrl = "https://itrade.gg/r/scmm")]
        [BuyFrom(Url = "https://itrade.gg/r/scmm?userInv={1}&botInv={1}", AcceptedPaymentTypes = PriceTypes.Trade)]
        iTradegg = 23,

        [Obsolete("Website is dead")]
        [Display(Name = "Trade Skins Fast")]
        [Market(Constants.RustAppId, Constants.CSGOAppId)]
        [BuyFrom(Url = "https://tradeskinsfast.com/", AcceptedPaymentTypes = PriceTypes.Trade)]
        TradeSkinsFast = 24,

        [Display(Name = "SkinsMonkey")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#F5C71B")]
        [BuyFrom(Url = "https://skinsmonkey.com/trade", AcceptedPaymentTypes = PriceTypes.Trade | PriceTypes.Cash | PriceTypes.Crypto)]
        SkinsMonkey = 25,

        [Display(Name = "Skin Swap")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#FF4B4B", AffiliateUrl = "https://skinswap.com/r/scmm")]
        [BuyFrom(Url = "https://skinswap.com/r/scmm", AcceptedPaymentTypes = PriceTypes.Trade | PriceTypes.Cash | PriceTypes.Crypto)]
        SkinSwap = 26,

        // TODO: Restricted to 100 items per query, too slow for CSGO items
        [Display(Name = "DMarket")]
        [Market(Constants.RustAppId, /*Constants.CSGOAppId,*/ Color = "#8dd294", AffiliateUrl = "https://dmarket.com?ref=6tlej6xqvD")]
        [BuyFrom(Url = "https://dmarket.com/ingame-items/item-list/{1}-skins?ref=6tlej6xqvD&exchangeTab=exchange&title={3}", AcceptedPaymentTypes = PriceTypes.Trade | PriceTypes.Cash | PriceTypes.Crypto)]
        DMarket = 28,

        // TODO: Restricted to 100 items per query, too slow for CSGO items
        [Display(Name = "DMarket - Face2Face")]
        [Market(Constants.RustAppId, /*Constants.CSGOAppId,*/ Color = "#8dd294")]
        [BuyFrom(Url = "https://dmarket.com/ingame-items/item-list/{1}-skins?ref=6tlej6xqvD&exchangeTab=f2fOffers&title={3}", AcceptedPaymentTypes = PriceTypes.Cash | PriceTypes.Crypto)]
        DMarketF2F = 29,

        // TODO: Restricted to 80 items per query, too slow for CSGO items
        [Display(Name = "BUFF")]
        [Market(Constants.RustAppId, /*Constants.CSGOAppId,*/ Color = "#FFFFFF")]
        [BuyFrom(Url = "https://buff.163.com/market/{1}#tab=selling&sort_by=price.asc&search={3}", AcceptedPaymentTypes = PriceTypes.Cash)]
        Buff = 30,

        [Display(Name = "Waxpeer")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#FFFFFF", AffiliateUrl = "https://waxpeer.com/r/scmm")]
        [BuyFrom(Url = "https://waxpeer.com/{1}?r=scmm&game={1}&sort=ASC&order=price&all=0&exact=0&search={3}", AcceptedPaymentTypes = PriceTypes.Trade | PriceTypes.Cash | PriceTypes.Crypto)] // Unconfirmed
        Waxpeer = 31,

        [Display(Name = "ShadowPay")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#005BBB", AffiliateUrl = "https://shadowpay.com?utm_campaign=e6PlQUT3mUC06NL")]
        [BuyFrom(Url = "https://shadowpay.com/en/{1}-items?utm_campaign=e6PlQUT3mUC06NL&price_from=0&price_to=500&currency=USD&search={3}&sort_column=price&sort_dir=asc", AcceptedPaymentTypes = PriceTypes.Trade | PriceTypes.Cash | PriceTypes.Crypto)] // Unconfirmed
        ShadowPay = 32,

        [Display(Name = "Mannco.store")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#5ad1e8")]
        [BuyFrom(Url = "https://mannco.store/rust?a=&b=&c=&d=&e={3}&f=ASC&g=&h=2&j=1&t=&s=&appid={0}", AcceptedPaymentTypes = PriceTypes.Trade | PriceTypes.Cash | PriceTypes.Crypto)] // Unconfirmed
        ManncoStore = 33,

        [Display(Name = "RapidSkins")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#ffd500", AffiliateUrl = "https://rapidskins.com/a/scmm")]
        [BuyFrom(Url = "https://rapidskins.com/a/scmm", AcceptedPaymentTypes = PriceTypes.Trade | PriceTypes.Cash | PriceTypes.Crypto)] // Unconfirmed
        RapidSkins = 34,

        /*
         
         MARKETS TO BE INVESTIGATED
         BUY:  https://skin.land/market/rust/
         BUY:  https://rustysaloon.com/withdraw (gambling site)
         BUY:  https://bandit.camp/ (gambling site)
         BUY:  https://buff.market/ (western sister-site for https://buff.163.com/, no rust)
         BUY:  https://gameflip.com/shop/in-game-items/rust?status=onsale&limit=100&platform=252490&sort=price%3Aasc (has low stock)
         BUY:  https://lis-skins.ru/market/rust/ (has overly aggressive CloudFlare WAF policies, need to scrap HTML code)
         BUY:  https://gamerall.com/rust (has overly aggressive CloudFlare WAF policies)
         SELL: https://rustysell.com/
         SELL: https://skincashier.com/
         SELL: https://skins.cash/

         SUS LOOKING MARKETS
         BUY:  https://trade.skin/
         BUY:  https://rustplus.com/
         BUY:  https://www.rustreaper.com/marketplace/RUST (bad reviews)

         MARKET AGGRAGATORS
         https://pricempire.com/api

        */

    }
}
