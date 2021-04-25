using SCMM.Steam.Data.Models.Enums;
using SCMM.Web.Data.Models.Steam;
using System;
using SCMM.Steam.Data.Models.Domain.Currencies;

namespace SCMM.Web.Data.Models.Domain.DTOs.MarketItems
{
    public class MarketItemListDTO : ISteamMarketListing, ISteamAssetStyles
    {
        public Guid Id { get; set; }

        public string SteamId { get; set; }

        public string SteamAppId { get; set; }

        public string SteamDescriptionId { get; set; }

        public string Name { get; set; }

        public string BackgroundColour { get; set; }

        public string ForegroundColour { get; set; }

        public string IconUrl { get; set; }

        public CurrencyDTO Currency { get; set; }

        public MarketItemActivityDTO[] Activity { get; set; }

        public int Supply { get; set; }

        public int Demand { get; set; }

        public long? StorePrice { get; set; }

        public long BuyAskingPrice { get; set; }

        public long BuyNowPrice { get; set; }

        public long ResellPrice { get; set; }

        public long ResellTax { get; set; }

        public long ResellProfit { get; set; }

        public long Last1hrSales { get; set; }

        public long Last1hrValue { get; set; }

        public long Last24hrSales { get; set; }

        public long Last24hrValue { get; set; }

        public long Last48hrSales { get; set; }

        public long Last48hrValue { get; set; }

        public long Last72hrSales { get; set; }

        public long Last72hrValue { get; set; }

        public long Last120hrSales { get; set; }

        public long Last120hrValue { get; set; }

        public long Last336hrSales { get; set; }

        public long Last336hrValue { get; set; }

        public long MovementLast48hrValue { get; set; }

        public long MovementLast120hrValue { get; set; }

        public long MovementLast336hrValue { get; set; }

        public long MovementAllTimeValue { get; set; }

        public long AllTimeAverageValue { get; set; }

        public long AllTimeHighestValue { get; set; }

        public long AllTimeLowestValue { get; set; }

        public bool? HasAppreciated => (StorePrice != null ? (bool?)(Last1hrValue >= StorePrice) : null);

        public bool? HasDepreciated => (StorePrice != null ? (bool?)(Last1hrValue < StorePrice) : null);

        public string MarketAge { get; set; }

        public int? Subscriptions { get; set; }

        public SteamMarketItemFlags Flags { get; set; }

        public SteamProfileMarketItemFlags ProfileFlags { get; set; }
    }
}
