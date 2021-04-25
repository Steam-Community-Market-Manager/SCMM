using SCMM.Steam.Data.Models.Domain.Currencies;

namespace SCMM.Web.Data.Models.UI.MarketStatistics
{
    public class DashboardAssetBuyOrderValueDTO : DashboardAssetDTO
    {
        public CurrencyDTO Currency { get; set; }

        public long BuyNowPrice { get; set; }

        public long BuyAskingPrice { get; set; }
    }
}
