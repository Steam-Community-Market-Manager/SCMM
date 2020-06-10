using System;

namespace SCMM.Steam.Shared.Community.Requests.Html
{
    public class SteamItemStorePageRequest : SteamRequest
    {
        public string AppId { get; set; }

        public Uri Uri => new Uri(
            $"{SteamConstants.SteamCommunityUrl}/itemstore/{Uri.EscapeUriString(AppId)}/"
        );
    }
}
