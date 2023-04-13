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
        [Market(Constants.RustAppId, Constants.CSGOAppId, Constants.UnturnedAppId, Color = "#171A21")]
        [BuyFrom(Url = "https://store.steampowered.com/itemstore/{0}/", AcceptedPaymentTypes = PriceTypes.Cash)]
        SteamStore = 1,

        [Display(Name = "Steam Community Market")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Constants.UnturnedAppId, Color = "#171A21")]
        [BuyFrom(Url = "https://steamcommunity.com/market/listings/{0}/{3}", AcceptedPaymentTypes = PriceTypes.Cash)]
        [SellTo(Url = "https://steamcommunity.com/market/listings/{0}/{3}", AcceptedPaymentTypes = PriceTypes.Cash, FeeRate = 13.0f)]
        SteamCommunityMarket = 2,

        [Display(Name = "Skinport")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#FA490A")]
        [BuyFrom(Url = "https://skinport.com/{1}/market?r=scmm&item={3}", AffiliateCode = "scmm", AcceptedPaymentTypes = PriceTypes.Cash)]
        Skinport = 10,

        [Display(Name = "LOOT.Farm")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#06FDDE")]
        [BuyFrom(Url = "https://loot.farm/", AcceptedPaymentTypes = PriceTypes.Trade | PriceTypes.Cash | PriceTypes.Crypto)]
        LOOTFarm = 11,

        [Display(Name = "Swap.gg")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#15C9AF")]
        [BuyFrom(Url = "https://swap.gg/?r=iHUYPlp5ehjhrD5DXf0FF&game={0}", AffiliateCode = "iHUYPlp5ehjhrD5DXf0FF", AcceptedPaymentTypes = PriceTypes.Trade | PriceTypes.Cash)]
        SwapGGTrade = 12,

        [Display(Name = "Swap.gg - Market")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#15C9AF")]
        [BuyFrom(Url = "https://market.swap.gg/{1}?r=iHUYPlp5ehjhrD5DXf0FF&search={3}", AffiliateCode = "iHUYPlp5ehjhrD5DXf0FF", AcceptedPaymentTypes = PriceTypes.Cash)]
        SwapGGMarket = 13,

        // TODO: Get API key to bypass web scraping
        [Obsolete("Aggressive CloudFlare anti-scrapping policies")]
        [Display(Name = "Tradeit.gg")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#27273F")]
        [BuyFrom(Url = "https://tradeit.gg/{1}/trade?aff=scmm&search={3}", AffiliateCode = "scmm", AcceptedPaymentTypes = PriceTypes.Trade | PriceTypes.Cash | PriceTypes.Crypto)]
        [BuyFrom(Url = "https://tradeit.gg/{1}/store?aff=scmm&search={3}", AffiliateCode = "scmm", AcceptedPaymentTypes = PriceTypes.Cash | PriceTypes.Crypto, DiscountMultiplier = 0.25f /* 25% */)]
        TradeitGG = 14,

        [Display(Name = "CS.Deals")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#78FFFF")]
        [BuyFrom(Url = "https://cs.deals/trade-skins?appid={0}", AcceptedPaymentTypes = PriceTypes.Trade | PriceTypes.Cash | PriceTypes.Crypto)]
        CSDealsTrade = 15,

        // TODO: Items quantities are not currently supported
        [Display(Name = "CS.Deals - Marketplace")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#78FFFF")]
        [BuyFrom(Url = "https://cs.deals/market/{1}/?name={3}&sort=price", AcceptedPaymentTypes = PriceTypes.Cash | PriceTypes.Crypto)]
        CSDealsMarketplace = 16,

        // TODO: Client code needs updating
        [Obsolete("Client code needs updating, currently broken")]
        [Display(Name = "Skin Baron")]
        [Market(Constants.CSGOAppId, Color = "#2A2745")]
        [BuyFrom(Url = "https://skinbaron.de/en/{1}?str={3}&sort=CF", AcceptedPaymentTypes = PriceTypes.Cash)]
        SkinBaron = 17,

        [Display(Name = "Rust.tm")]
        [Market(Constants.RustAppId, Color = "#4E2918")]
        [BuyFrom(Url = "https://rust.tm/?s=price&t=all&search={3}&sd=asc", AcceptedPaymentTypes = PriceTypes.Cash | PriceTypes.Crypto)] // Unconfirmed
        RustTM = 19,

        [Display(Name = "CS.TRADE")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#F3C207")]
        [BuyFrom(Url = "https://cs.trade/ref/SCMM#trader", AffiliateCode = "SCMM", AcceptedPaymentTypes = PriceTypes.Trade | PriceTypes.Cash | PriceTypes.Crypto)] // Unconfirmed
        CSTRADE = 22,

        [Display(Name = "iTrade.gg")]
        [Market(Constants.RustAppId, Color = "#EA473B")]
        [BuyFrom(Url = "https://itrade.gg/r/scmm?userInv={1}&botInv={1}", AffiliateCode = "scmm", AcceptedPaymentTypes = PriceTypes.Trade)] // Unconfirmed
        iTradegg = 23,

        [Display(Name = "SkinsMonkey")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#F5C71B")]
        [BuyFrom(Url = "https://skinsmonkey.com/trade", AcceptedPaymentTypes = PriceTypes.Trade | PriceTypes.Cash | PriceTypes.Crypto)] // Unconfirmed
        SkinsMonkey = 25,

        [Display(Name = "Skin Swap")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#FF4B4B")]
        [BuyFrom(Url = "https://skinswap.com/r/scmm", AffiliateCode = "scmm", AcceptedPaymentTypes = PriceTypes.Trade | PriceTypes.Cash | PriceTypes.Crypto)] // Unconfirmed
        SkinSwap = 26,

        // TODO: Restricted to 100 items per query, slow
        [Display(Name = "DMarket")]
        [Market(Constants.RustAppId, /*Constants.CSGOAppId,*/ Color = "#8dd294")]
        [BuyFrom(Url = "https://dmarket.com/ingame-items/item-list/{1}-skins?ref=6tlej6xqvD&exchangeTab=exchange&title={3}", AffiliateCode = "6tlej6xqvD", AcceptedPaymentTypes = PriceTypes.Trade | PriceTypes.Cash | PriceTypes.Crypto)]
        DMarket = 28,

        // TODO: Restricted to 100 items per query, slow
        [Display(Name = "DMarket - Face2Face")]
        [Market(Constants.RustAppId, /*Constants.CSGOAppId,*/ Color = "#8dd294")]
        [BuyFrom(Url = "https://dmarket.com/ingame-items/item-list/{1}-skins?ref=6tlej6xqvD&exchangeTab=f2fOffers&title={3}", AffiliateCode = "6tlej6xqvD", AcceptedPaymentTypes = PriceTypes.Cash | PriceTypes.Crypto)]
        DMarketF2F = 29,

        // TODO: Restricted to 80 items per query, slow
        [Display(Name = "BUFF")]
        [Market(Constants.RustAppId, /*Constants.CSGOAppId,*/ Color = "#FFFFFF")]
        [BuyFrom(Url = "https://buff.163.com/market/{1}#tab=selling&sort_by=price.asc&search={3}", AcceptedPaymentTypes = PriceTypes.Cash)] // Unconfirmed
        Buff = 30,

        /*
         * https://docs.waxpeer.com/?method=prices-get
         * https://api.waxpeer.com/v1/prices?game=rust&minified=1
        [Display(Name = "Waxpeer")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#FFFFFF")]
        [BuyFrom(Url = "https://waxpeer.com/rust/r/scmm?game={1}&sort=ASC&order=price&all=0&exact=0&search={3}", AffiliateCode = "scmm", AcceptedPaymentTypes = PriceTypes.Crypto)] // Unconfirmed
        Waxpeer = 31,
        */

        /*
         * https://doc.shadowpay.com/docs/shadowpay/96108be6ddc1e-get-items-on-sale
         * https://api.shadowpay.com/api/market/get_items?currency=USD&sort_column=price&sort_dir=asc&stack=false&offset=0&limit=50&sort=asc&game=rust
        [Display(Name = "ShadowPay")]
        [Market(Constants.RustAppId, Constants.CSGOAppId, Color = "#005BBB")]
        [BuyFrom(Url = "https://shadowpay.com/en/{1}-items?utm_campaign=e6PlQUT3mUC06NL&price_from=0&price_to=500&currency=USD&search={3}&sort_column=price&sort_dir=asc", AffiliateCode = "e6PlQUT3mUC06NL", AcceptedPaymentTypes = PriceTypes.Trade | PriceTypes.Cash | PriceTypes.Crypto)] // Unconfirmed
        ShadowPay = 32,
        */

        /*
        BUY:  https://lis-skins.ru/market/rust/
        BUY:  https://mannco.store/market
        BUY:  https://gameflip.com/shop/in-game-items/rust?status=onsale&limit=100&platform=252490&sort=price%3Aasc
        BUY:  https://buff.market/ (is this an alias for https://buff.163.com/?)
        BUY:  https://gamerall.com/rust (has overly aggressive CloudFlare WAF policies)
        BUY:  https://www.rustreaper.com/marketplace/RUST
        BUY:  https://rustysaloon.com/withdraw
        BUY:  https://bandit.camp/
        BUY:  https://trade.skin/ (looks sus...)
        BUY:  https://rustplus.com/ (looks sus...)
        SELL: https://rustysell.com/
        SELL: https://skincashier.com/
        SELL: https://skins.cash/
        */

        #region Deprecated Markets

        //[Display(Name = "RUST Skins")]
        //[Market(Constants.RustAppId, Color = "#EF7070")]
        //[BuyFrom(Url = "https://rustskins.com/market?search={3}&sort=p-ascending", AcceptedPaymentTypes = PriceTypes.Cash)]
        [Obsolete("Domain is dead. Unable to deposit cash. Social links are dead")]
        RUSTSkins = 18,

        //[Display(Name = "RUSTVendor")]
        //[Market(Constants.RustAppId)]
        //[BuyFrom(Url = "https://rustvendor.com/trade", AcceptedPaymentTypes = PriceTypes.Trade | PriceTypes.Cash)]
        [Obsolete("Domain is dead. Unable to deposit cash. Social links are dead")]
        RUSTVendor = 20,

        // TODO: Implement web socket client support
        //       wss://rustytrade.com/socket.io/?EIO=3&transport=websocket&sid=xxx
        //          => 42["get bots inv"]
        //          <= 42["bots inv",…]
        //[Display(Name = "RustyTrade")]
        //[Market(Constants.RustAppId)]
        //[BuyFrom(Url = "https://rustytrade.com/", AcceptedPaymentTypes = PriceTypes.Trade)] // Unconfirmed
        [Obsolete("Very inactive website, bot inventory doesn't load")]
        RustyTrade = 21,

        //[Display(Name = "Trade Skins Fast")]
        //[Market(Constants.RustAppId, Constants.CSGOAppId)]
        //[BuyFrom(Url = "https://tradeskinsfast.com/", AcceptedPaymentTypes = PriceTypes.Trade)]
        [Obsolete("Domain is dead. Redirects to CS.Deals")]
        TradeSkinsFast = 24,

        #endregion
    }
}
