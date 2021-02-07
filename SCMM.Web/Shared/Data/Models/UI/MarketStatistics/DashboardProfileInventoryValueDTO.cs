using SCMM.Web.Shared.Domain.DTOs.Currencies;

namespace SCMM.Web.Shared.Data.Models.UI.MarketStatistics
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
