using System;

namespace SCMM.Steam.Shared.Requests
{
    public class SteamInventoryRequest : SteamRequest
    {
        public string SteamId { get; set; }

        public string AppId { get; set; }
            
        public string AssetId { get; set; }

        public Uri Uri => new Uri(String.IsNullOrEmpty(AssetId)
            ? $"{SteamConstants.SteamCommunityUrl}/profiles/{Uri.EscapeDataString(SteamId)}/inventory/#{Uri.EscapeDataString(AppId)}"
            : $"{SteamConstants.SteamCommunityUrl}/profiles/{Uri.EscapeDataString(SteamId)}/inventory/#{Uri.EscapeDataString(AppId)}_2_{AssetId}"
        );
    }
}
