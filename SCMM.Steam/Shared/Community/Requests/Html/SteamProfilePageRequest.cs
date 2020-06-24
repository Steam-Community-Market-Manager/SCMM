using System;

namespace SCMM.Steam.Shared.Community.Requests.Html
{
    public class SteamProfilePageRequest : SteamRequest
    {
        public string ProfileId { get; set; }

        public bool Xml { get; set; }

        public override Uri Uri => new Uri(
            $"{SteamConstants.SteamCommunityUrl}/id/{Uri.EscapeDataString(ProfileId)}/?xml={(Xml ? "1" : "0")}"
        );
    }
}
