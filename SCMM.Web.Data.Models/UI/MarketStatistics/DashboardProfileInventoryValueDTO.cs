using SCMM.Steam.Data.Models.Domain.Currencies;

namespace SCMM.Web.Data.Models.UI.MarketStatistics
{
    public class DashboardProfileInventoryValueDTO
    {
        public string SteamId { get; set; }

        public string Name { get; set; }

        public string AvatarUrl { get; set; }

        public CurrencyDTO Currency { get; set; }

        public int TotalItems { get; set; }

        public long MarketValue { get; set; }
    }
}
