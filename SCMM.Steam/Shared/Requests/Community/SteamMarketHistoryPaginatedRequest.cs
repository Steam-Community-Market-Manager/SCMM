using System;

namespace SCMM.Steam.Shared.Requests.Community
{
    public class SteamMarketHistoryPaginatedRequest : SteamPaginatedRequest
    {
        public const int MaxPageSize = 500;

        public Uri Uri => new Uri(
            $"{SteamConstants.SteamCommunityUrl}/market/myhistory?start={Start}&count={Count}&language={Uri.EscapeDataString(Language)}&currency={Uri.EscapeDataString(CurrencyId)}&norender={(NoRender ? "1" : "0")}"
        );
    }
}
