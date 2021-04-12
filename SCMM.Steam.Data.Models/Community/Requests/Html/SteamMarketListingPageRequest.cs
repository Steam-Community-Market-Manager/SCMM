using System;

namespace SCMM.Steam.Data.Models.Community.Requests.Html
{
    public class SteamMarketListingPageRequest : SteamRequest
    {
        public string AppId { get; set; }

        public string MarketHashName { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamCommunityUrl}/market/listings/{Uri.EscapeDataString(AppId ?? String.Empty)}/{Uri.EscapeDataString(MarketHashName ?? String.Empty)}"
        );
    }
}
