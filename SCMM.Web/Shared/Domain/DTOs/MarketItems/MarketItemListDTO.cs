using System;
using System.Collections.Generic;

namespace SCMM.Web.Shared.Domain.DTOs.MarketItems
{
    public class MarketItemListDTO
    {
        public Guid Id { get; set; }

        public string SteamId { get; set; }

        public string SteamAppId { get; set; }

        public string Name { get; set; }

        public string BackgroundColour { get; set; }

        public string ForegroundColour { get; set; }

        public string IconUrl { get; set; }

        public CurrencyDTO Currency { get; set; }

        public int Supply { get; set; }

        public int Demand { get; set; }

        public int BuyAskingPrice { get; set; }

        public int BuyNowPrice { get; set; }

        public int ResellPrice { get; set; }

        public int ResellTax { get; set; }

        public int ResellProfit { get; set; }

        public int First24hrValue { get; set; }

        public int Last24hrSales { get; set; }

        public int Last24hrValue { get; set; }

        public int Last48hrSales { get; set; }

        public int Last48hrValue { get; set; }

        public int Last120hrSales { get; set; }

        public int Last120hrValue { get; set; }

        public int Last336hrSales { get; set; }

        public int Last336hrValue { get; set; }

        public int MovementLast48hrValue { get; set; }

        public int MovementLast120hrValue { get; set; }

        public int MovementLast336hrValue { get; set; }

        public int MovementAllTimeValue { get; set; }

        public int AllTimeHighestValue { get; set; }

        public int AllTimeLowestValue { get; set; }

        public bool HasAppreciated { get; set; }

        public bool HasDepreciated { get; set; }

        public string MarketAge { get; set; }

        public int Subscriptions { get; set; }

        public IDictionary<string, string> Tags { get; set; }
    }
}
