using System;

namespace SCMM.Steam.Shared.Community.Requests.Json
{
    public class SteamMarketMyListingsPaginatedJsonRequest : SteamPaginatedJsonRequest
    {
        public const int MaxPageSize = 100;

        public override Uri Uri => new Uri(
            $"{SteamConstants.SteamCommunityUrl}/market/mylistings?start={Start}&count={Count}&language={Uri.EscapeDataString(Language)}&currency={Uri.EscapeDataString(CurrencyId)}&norender={(NoRender ? "1" : "0")}"
        );
    }
}
