using System;

namespace SCMM.Steam.Shared.Requests.Community.Html
{
    public class SteamWorkshopBrowsePageRequest : SteamRequest
    {
        public const string BrowseSortMostPopular = "mostpopular";
        public const string BrowseSortMostRecent = "mostrecent";
        public const string SectionItems = "mtxitems";

        public string AppId { get; set; }

        public string BrowseSort { get; set; } = BrowseSortMostPopular;

        public string Section { get; set; } = SectionItems;

        public Uri Uri => new Uri(
            $"{SteamConstants.SteamCommunityUrl}/workshop/browse/?appid={Uri.EscapeDataString(AppId)}&browsesort={Uri.EscapeDataString(BrowseSort)}&section={Uri.EscapeDataString(Section)}"
        );
    }
}
