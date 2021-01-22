using SCMM.Web.Shared.Domain.DTOs.Currencies;

namespace SCMM.Web.Shared.Data.Models.UI.MarketStatistics
{
    public class DashboardAssetBuyOrderValueDTO : DashboardAssetDTO
    {
        public CurrencyDTO Currency { get; set; }

        public long BuyNowPrice { get; set; }

        public long BuyAskingPrice { get; set; }
    }
}
