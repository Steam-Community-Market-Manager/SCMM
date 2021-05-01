using System;
using SCMM.Web.Data.Models.Domain.Currencies;

namespace SCMM.Web.Data.Models.UI.ProfileInventory
{
    public class InventoryInvestmentItemDTO : IFilterableItem
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

        public string BuyPriceLocal { get; set; }

        public int Quantity { get; set; }

        public long? Last1hrValue { get; set; }

        public long? ResellPrice { get; set; }

        public long? ResellTax { get; set; }

        public long? ResellProfit { get; set; }
    }
}
