using System;

namespace SCMM.Steam.Shared.Community.Requests.Html
{
    public class SteamItemStorePageRequest : SteamRequest
    {
        public const int MaxPageSize = 100;

        public const string FilterFeatured = "Featured";
        public const string FilterAll = "All";

        public string AppId { get; set; }

        public int Start { get; set; }

        public int Count { get; set; }

        public string Filter { get; set; }

        public string SearchText { get; set; }

        public override Uri Uri => new Uri(
            $"{SteamConstants.SteamStoreUrl}/itemstore/{Uri.EscapeUriString(AppId)}/?start={Start}&count={Count}&filter={Uri.EscapeUriString(Filter ?? String.Empty)}&searchtext={Uri.EscapeUriString(SearchText ?? String.Empty)}"
        );
    }
}
