using System;

namespace SCMM.Steam.Data.Models.Community.Requests.Html
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
            $"{Constants.SteamStoreUrl}/itemstore/{Uri.EscapeDataString(AppId)}/?start={(Start > 0 ? Start.ToString() : String.Empty)}&count={(Count > 0 ? Count.ToString() : String.Empty)}&filter={Uri.EscapeDataString(Filter ?? String.Empty)}&searchtext={Uri.EscapeDataString(SearchText ?? String.Empty)}"
        );
    }
}
