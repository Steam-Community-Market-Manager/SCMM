using System;

namespace SCMM.Steam.Shared.Community.Requests.Html
{
    public class SteamProfilePageRequest : SteamRequest
    {
        public string SteamId { get; set; }

        public bool Xml { get; set; }

        public override Uri Uri => new Uri(
            $"{SteamConstants.SteamCommunityUrl}/profiles/{Uri.EscapeDataString(SteamId)}/?xml={(Xml ? "1" : "0")}"
        );
    }
}
