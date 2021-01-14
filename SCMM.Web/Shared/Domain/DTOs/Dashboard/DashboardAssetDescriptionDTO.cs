using SCMM.Web.Shared.Domain.DTOs.InventoryItems;
using System;

namespace SCMM.Web.Shared.Domain.DTOs.Dashboard
{
    public class DashboardAssetDescriptionDTO : ISteamMarketListing, ISteamAssetStyles
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
