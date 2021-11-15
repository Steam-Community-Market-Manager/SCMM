using SCMM.Steam.Data.Models.Enums;
using SCMM.Web.Data.Models.UI.Currency;

namespace SCMM.Web.Data.Models.UI.Profile.Inventory
{
    public class InventoryInvestmentItemDTO : IItemDescription
    {
        public Guid Guid { get; set; }

        public ulong Id { get; set; }

        public ulong AppId { get; set; }

        public string Name { get; set; }

        public string ItemType { get; set; }

        public string BackgroundColour { get; set; }

        public string ForegroundColour { get; set; }

        public string IconUrl { get; set; }

        public DateTimeOffset? TimeAccepted { get; set; }

        public SteamProfileInventoryItemAcquisitionType AcquiredBy { get; set; }

        /// <summary>
        /// The original user-provided buy currency
        /// </summary>
        public CurrencyDetailedDTO BuyCurrency { get; set; }

        /// <summary>
        /// The original user-provided buy price, in the original user-provided currency
        /// </summary>
        public long? BuyPrice { get; set; }

        // TODO: Remove this, needed for client-side binding
        public string BuyPriceText { get; set; }

        /// <summary>
        /// The buy price, in the callers local currency
        /// </summary>
        public long? BuyPriceLocal { get; set; }

        /// <summary>
        /// The original store price, in the callers local currency
        /// </summary>
        public long? BuyPriceStore { get; set; }

        public int Quantity { get; set; }

        public long? ResellPrice { get; set; }

        public long? ResellTax { get; set; }
    }
}
