using System;

namespace SCMM.Steam.Shared.Community.Requests.Html
{
    public class SteamWorkshopBrowsePageRequest : SteamRequest
    {
        public const string BrowseSortMostPopular = "mostpopular";
        public const string BrowseSortMostRecent = "mostrecent";
        public const string SectionItems = "mtxitems";

        public string AppId { get; set; }

        public string BrowseSort { get; set; } = BrowseSortMostPopular;

        public string Section { get; set; } = SectionItems;

        public override Uri Uri => new Uri(
            $"{SteamConstants.SteamCommunityUrl}/workshop/browse/?appid={Uri.EscapeDataString(AppId)}&browsesort={Uri.EscapeDataString(BrowseSort)}&section={Uri.EscapeDataString(Section)}"
        );
    }
}
