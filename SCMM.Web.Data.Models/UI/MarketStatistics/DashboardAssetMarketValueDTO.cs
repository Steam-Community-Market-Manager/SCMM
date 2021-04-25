using SCMM.Steam.Data.Models.Domain.Currencies;

namespace SCMM.Web.Data.Models.UI.MarketStatistics
{
    public class DashboardAssetMarketValueDTO : DashboardAssetDTO
    {
        public CurrencyDTO Currency { get; set; }

        public long Last1hrValue { get; set; }
    }
}
