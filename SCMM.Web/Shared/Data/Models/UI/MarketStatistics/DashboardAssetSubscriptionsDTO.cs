using SCMM.Web.Shared.Data.Models.Steam;
using System;

namespace SCMM.Web.Shared.Data.Models.UI.MarketStatistics
{
    public class DashboardAssetSubscriptionsDTO : ISteamMarketListing, ISteamAssetStyles
    {
        public string SteamId { get; set; }

        public string SteamAppId { get; set; }

        public string Name { get; set; }

        public string BackgroundColour { get; set; }

        public string ForegroundColour { get; set; }

        public string IconUrl { get; set; }

        public int Subscriptions { get; set; }
    }
}
