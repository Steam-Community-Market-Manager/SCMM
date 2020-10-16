using System;

namespace SCMM.Steam.Shared.Community.Requests.Html
{
    public class SteamMarketListingPageRequest : SteamRequest
    {
        public string AppId { get; set; }

        public string MarketHashName { get; set; }

        public override Uri Uri => new Uri(
            $"{SteamConstants.SteamCommunityUrl}/market/listings/{Uri.EscapeDataString(AppId ?? String.Empty)}/{Uri.EscapeUriString(MarketHashName?.Replace(" ", "%20") ?? String.Empty)}"
        );
    }
}
