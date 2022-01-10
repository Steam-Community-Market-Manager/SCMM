using SCMM.Steam.Data.Models.Enums;

namespace SCMM.Web.Data.Models.UI
{
    public interface ICanBePurchased
    {
        public MarketType? BuyNowFrom { get; }

        public string BuyNowUrl { get; }

        public long? BuyNowPrice { get; }

        public long? OriginalPrice { get; }

        public long? Demand { get; }

        public long? Supply { get; }
    }
}
