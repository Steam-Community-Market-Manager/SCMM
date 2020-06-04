using System;

namespace SCMM.Steam.Shared.Requests
{
    public class SteamMarketAppFiltersRequest : SteamRequest
    {
        public string AppId { get; set; }

        public Uri Uri => new Uri(
            $"{SteamConstants.SteamCommunityUrl}/market/appfilters/#{Uri.EscapeDataString(AppId)}"
        );
    }
}
