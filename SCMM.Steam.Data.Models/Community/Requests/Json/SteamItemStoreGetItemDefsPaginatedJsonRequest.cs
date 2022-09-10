namespace SCMM.Steam.Data.Models.Community.Requests.Json
{
    public class SteamItemStoreGetItemDefsPaginatedJsonRequest : SteamPaginatedJsonRequest
    {
        public const int MaxPageSize = 100;

        public const string FilterFeatured = "Featured";
        public const string FilterAll = "All";

        public string AppId { get; set; }

        public string Filter { get; set; } = FilterAll;

        public string SearchText { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamStoreUrl}/itemstore/{Uri.EscapeDataString(AppId)}/ajaxgetitemdefs?start={Start}&count={Count}&norender={(NoRender ? "1" : "0")}&filter={Uri.EscapeDataString(Filter)}&searchtext={Uri.EscapeDataString(SearchText)}"
        );
    }
}
