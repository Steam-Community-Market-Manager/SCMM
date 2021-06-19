using SCMM.Steam.Data.Models.Enums;

namespace SCMM.Web.Data.Models.UI
{
    public interface ICanBePurchased
    {
        public PriceType? BuyNowFrom { get; }

        public long? BuyNowPrice { get; }

        public string BuyNowUrl { get; }
    }
}
