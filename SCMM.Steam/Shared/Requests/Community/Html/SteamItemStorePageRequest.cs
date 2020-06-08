using System;

namespace SCMM.Steam.Shared.Requests.Community.Html
{
    public class SteamItemStorePageRequest : SteamRequest
    {
        public string AppId { get; set; }

        public Uri Uri => new Uri(
            $"{SteamConstants.SteamCommunityUrl}/itemstore/{Uri.EscapeUriString(AppId)}/"
        );
    }
}
