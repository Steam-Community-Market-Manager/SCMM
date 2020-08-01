using System;

namespace SCMM.Steam.Shared.Community.Requests.Json
{
    public class SteamItemStorePaginatedJsonRequest : SteamPaginatedJsonRequest
    {
        public const int MaxPageSize = 100;

        public const string FilterFeatured = "Featured";
        public const string FilterAll = "All";

        public string AppId { get; set; }

        public string Filter { get; set; }

        public string SearchText { get; set; }

        public override Uri Uri => new Uri(
            $"{SteamConstants.SteamStoreUrl}/itemstore/{Uri.EscapeUriString(AppId)}/ajaxgetitemdefs?start={Start}&count={Count}&norender={(NoRender ? "1" : "0")}&filter={Uri.EscapeUriString(Filter)}&searchtext={Uri.EscapeUriString(SearchText)}"
        );
    }
}
