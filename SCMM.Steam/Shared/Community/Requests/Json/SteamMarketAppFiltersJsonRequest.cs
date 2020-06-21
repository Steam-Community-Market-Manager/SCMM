using System;

namespace SCMM.Steam.Shared.Community.Requests.Json
{
    public class SteamMarketAppFiltersJsonRequest : SteamRequest
    {
        public string AppId { get; set; }

        public override Uri Uri => new Uri(
            $"{SteamConstants.SteamCommunityUrl}/market/appfilters/{Uri.EscapeDataString(AppId)}"
        );
    }
}
