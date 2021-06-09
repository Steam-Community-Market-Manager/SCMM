using SCMM.Steam.Data.Models.Enums;
using SCMM.Web.Data.Models.Domain.Currencies;

namespace SCMM.Web.Data.Models.UI
{
    public interface IPurchasable
    {
        public PriceType? BuyNowFrom { get; }

        public CurrencyDTO BuyNowCurrency { get; }

        public long? BuyNowPrice { get; }

        public string BuyNowUrl { get; }
    }
}
