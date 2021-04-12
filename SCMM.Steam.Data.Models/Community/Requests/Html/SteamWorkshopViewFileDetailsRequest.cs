using System;

namespace SCMM.Steam.Data.Models.Community.Requests.Html
{
    public class SteamWorkshopViewFileDetailsRequest : SteamRequest
    {
        public string Id { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamCommunityUrl}/sharedfiles/filedetails/?id={Uri.EscapeDataString(Id)}"
        );
    }
}
