using SCMM.Steam.Data.Models.Enums;
using SCMM.Web.Data.Models.Domain.Currencies;
using System;
using System.Text.Json.Serialization;

namespace SCMM.Web.Data.Models.UI.ProfileInventory
{
    public class InventoryInvestmentItemDTO : IItemDescription, ISearchable
    {
        public Guid Id { get; set; }

        public string SteamId { get; set; }

        public string SteamAppId { get; set; }

        public string Name { get; set; }

        public string ItemType { get; set; }

        public string BackgroundColour { get; set; }

        public string ForegroundColour { get; set; }

        public string IconUrl { get; set; }

        public SteamProfileInventoryItemAcquisitionType AcquiredBy { get; set; }

        /// <summary>
        /// The original user-provided buy currency
        /// </summary>
        public CurrencyDetailedDTO Currency { get; set; }

        /// <summary>
        /// The original user-provided buy price, in the original user-provided currency
        /// </summary>
        public long? BuyPrice { get; set; }

        /// <summary>
        /// The buy price, in the callers local currency
        /// </summary>
        public long? BuyPriceLocal { get; set; }

        /// <summary>
        /// The original store price, in the callers local currency
        /// </summary>
        public long? BuyPriceStore { get; set; }

        public string BuyPriceText { get; set; }

        public int Quantity { get; set; }

        public long? Last1hrValue { get; set; }

        public long? ResellPrice { get; set; }

        public long? ResellTax { get; set; }

        public long? ResellProfit { get; set; }

        [JsonIgnore]
        public object[] SearchData => new object[] { SteamId, Name };
    }
}
