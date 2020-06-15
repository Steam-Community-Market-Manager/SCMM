using System;

namespace SCMM.Web.Shared.Domain.DTOs.Steam
{
    public class SteamMarketItemDTO : SteamItemDTO
    {
        public SteamCurrencyDTO Currency { get; set; }

        public SteamMarketItemOrderDTO[] BuyOrders { get; set; }

        public SteamMarketItemOrderDTO[] SellOrders { get; set; }

        public SteamMarketItemSaleDTO[] SalesHistory { get; set; }

        public int Supply { get; set; }

        public int Demand { get; set; }

        public int BuyAskingPrice { get; set; }

        public int BuyNowPrice { get; set; }

        public int BuyNowPriceDelta { get; set; }

        public int ResellPrice { get; set; }

        public int ResellTax { get; set; }

        public int ResellProfit { get; set; }

        public bool WouldResellProfit { get; set; }

        public bool WouldResellLoss { get; set; }

        public int First24hrValue { get; set; }

        public int Last24hrSales { get; set; }

        public int Last24hrValue { get; set; }

        public int Last48hrValue { get; set; }

        public int Last120hrValue { get; set; }

        public bool IsStonking { get; set; }

        public bool IsStinking { get; set; }

        public int MovementLast48hrValue { get; set; }

        public int MovementLast120hrValue { get; set; }

        public int MovementAllTimeValue { get; set; }

        public bool HasAppreciated { get; set; }

        public bool HasDepreciated { get; set; }

        public int AllTimeHighestValue { get; set; }

        public DateTimeOffset? AllTimeHighestValueOn { get; set; }

        public int AllTimeLowestValue { get; set; }

        public DateTimeOffset? AllTimeLowestValueOn { get; set; }

        public int AllTimeSwingValue { get; set; }
        
        public DateTimeOffset? FirstSeenOn { get; set; }

        public string MarketAge { get; set; }

        public DateTimeOffset? LastCheckedOn { get; set; }
    }
}
