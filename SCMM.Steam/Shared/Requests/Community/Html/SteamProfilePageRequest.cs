using System;

namespace SCMM.Steam.Shared.Requests.Community.Html
{
    public class SteamProfilePageRequest : SteamRequest
    {
        public string SteamId { get; set; }

        public bool Xml { get; set; }

        public Uri Uri => new Uri(
            $"{SteamConstants.SteamCommunityUrl}/profiles/{Uri.EscapeDataString(SteamId)}/?xml={(Xml ? "1" : "0")}"
        );
    }
}
