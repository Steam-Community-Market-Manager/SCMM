using System;

namespace SCMM.Steam.Shared.Requests.Community
{
    public class SteamMarketListingRequest : SteamRequest
    {
        public string AppId { get; set; }

        public string MarketHashName { get; set; }

        public Uri Uri => new Uri(
            $"{SteamConstants.SteamCommunityUrl}/market/listings/{Uri.EscapeDataString(AppId)}/{Uri.EscapeDataString(MarketHashName)}"
        );
    }
}
