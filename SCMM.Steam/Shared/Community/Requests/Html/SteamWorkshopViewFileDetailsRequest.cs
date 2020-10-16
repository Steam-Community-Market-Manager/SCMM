using System;

namespace SCMM.Steam.Shared.Community.Requests.Html
{
    public class SteamWorkshopViewFileDetailsRequest : SteamRequest
    {
        public string Id { get; set; }

        public override Uri Uri => new Uri(
            $"{SteamConstants.SteamCommunityUrl}/sharedfiles/filedetails/?id={Uri.EscapeDataString(Id)}"
        );
    }
}
