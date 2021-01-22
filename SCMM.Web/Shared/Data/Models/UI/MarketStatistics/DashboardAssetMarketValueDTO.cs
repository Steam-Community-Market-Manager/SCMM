using SCMM.Web.Shared.Domain.DTOs.Currencies;

namespace SCMM.Web.Shared.Data.Models.UI.MarketStatistics
{
    public class DashboardAssetMarketValueDTO : DashboardAssetDTO
    {
        public CurrencyDTO Currency { get; set; }

        public long Last1hrValue { get; set; }
    }
}
