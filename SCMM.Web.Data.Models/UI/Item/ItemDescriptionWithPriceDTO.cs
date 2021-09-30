using SCMM.Steam.Data.Models.Enums;

namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemDescriptionWithPriceDTO : ItemDescriptionDTO, ICanBeSubscribed, ICanBePurchased
    {
        public long? OriginalPrice { get; set; }

        public PriceType? BuyNowFrom { get; set; }

        public long? BuyNowPrice { get; set; }

        public string BuyNowUrl { get; set; }

        public long? Subscriptions { get; set; }

        public long? Supply { get; set; }

        public long? Demand  { get; set; }
    }
}
