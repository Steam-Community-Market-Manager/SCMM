using SCMM.Steam.Data.Models.Enums;

namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemDescriptionWithPriceDTO : IItemDescription, ICanBePurchased, ICanBeSubscribed
    {
        public ulong Id { get; set; }

        public ulong AppId { get; set; }

        public string Name { get; set; }

        public string ItemType { get; set; }

        public string BackgroundColour { get; set; }

        public string ForegroundColour { get; set; }

        public string IconUrl { get; set; }

        public PriceType? BuyNowFrom { get; set; }

        public long? BuyNowPrice { get; set; }

        public string BuyNowUrl { get; set; }

        public long? OriginalPrice { get; set; }

        public long? Supply { get; set; }

        public long? Subscriptions { get; set; }
    }
}
