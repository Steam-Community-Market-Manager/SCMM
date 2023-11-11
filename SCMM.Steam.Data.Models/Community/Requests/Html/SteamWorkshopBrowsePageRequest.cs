namespace SCMM.Steam.Data.Models.Community.Requests.Html
{
    public class SteamWorkshopBrowsePageRequest : SteamRequest
    {
        public const string BrowseSortAccepted = "accepted";
        public const string BrowseSortMostPopular = "mostpopular";
        public const string BrowseSortMostRecent = "mostrecent";
        public const string SectionItems = "mtxitems";
        public const string SectionAccepted = "accepted";

        public string AppId { get; set; }

        public string BrowseSort { get; set; } = BrowseSortMostPopular;

        public string Section { get; set; } = SectionItems;

        public override Uri Uri => new Uri(
            $"{Constants.SteamCommunityUrl}/workshop/browse/?appid={Uri.EscapeDataString(AppId)}&browsesort={Uri.EscapeDataString(BrowseSort)}&section={Uri.EscapeDataString(Section)}"
        );
    }
}
