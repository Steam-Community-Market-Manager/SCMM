using System;

namespace SCMM.Steam.Shared.Requests.Community.Json
{
    public class SteamMarketAppFiltersJsonRequest : SteamRequest
    {
        public string AppId { get; set; }

        public Uri Uri => new Uri(
            $"{SteamConstants.SteamCommunityUrl}/market/appfilters/#{Uri.EscapeDataString(AppId)}"
        );
    }
}
