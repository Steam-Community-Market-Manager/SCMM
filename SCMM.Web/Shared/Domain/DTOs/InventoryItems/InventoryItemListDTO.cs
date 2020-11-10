using SCMM.Web.Shared.Domain.DTOs.Currencies;
using System;

namespace SCMM.Web.Shared.Domain.DTOs.InventoryItems
{
    public class InventoryItemListDTO : IFilterableItem
    {
        public Guid Id { get; set; }

        public string SteamId { get; set; }

        public string SteamAppId { get; set; }

        public string Name { get; set; }

        public string BackgroundColour { get; set; }

        public string ForegroundColour { get; set; }

        public string IconUrl { get; set; }

        public CurrencyDTO Currency { get; set; }

        public long? BuyPrice { get; set; }

        public int Quantity { get; set; }

        public int? Supply { get; set; }

        public int? Demand { get; set; }

        public long? BuyAskingPrice { get; set; }

        public long? BuyNowPrice { get; set; }

        public long? ResellPrice { get; set; }

        public long? ResellTax { get; set; }

        public long? ResellProfit { get; set; }

        public long? Last1hrValue { get; set; }
    }
}
