using SCMM.Steam.Data.Models.Enums;
using SCMM.Web.Data.Models.UI.Currency;
using SCMM.Web.Data.Models.UI.Item;

namespace SCMM.Web.Data.Models.UI.Profile.Inventory
{
    public class ProfileInventoryInvestmentItemDTO : ItemDescriptionDTO
    {
        public Guid Guid { get; set; }

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
