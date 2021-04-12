using SCMM.Web.Data.Models.Steam;

namespace SCMM.Web.Data.Models.UI.MarketStatistics
{
    public class DashboardAssetDTO : ISteamMarketListing, ISteamAssetStyles
    {
        public string SteamId { get; set; }

        public string SteamAppId { get; set; }

        public string Name { get; set; }

        public string BackgroundColour { get; set; }

        public string ForegroundColour { get; set; }

        public string IconUrl { get; set; }
    }
}
