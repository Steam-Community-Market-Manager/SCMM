using System;

namespace SCMM.Steam.Shared.Requests.Community.Json
{
    public class SteamMarketMyListingsPaginatedJsonRequest : SteamPaginatedJsonRequest
    {
        public const int MaxPageSize = 100;

        public Uri Uri => new Uri(
            $"{SteamConstants.SteamCommunityUrl}/market/mylistings?start={Start}&count={Count}&language={Uri.EscapeDataString(Language)}&currency={Uri.EscapeDataString(CurrencyId)}&norender={(NoRender ? "1" : "0")}"
        );
    }
}
