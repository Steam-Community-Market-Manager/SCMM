using System;

namespace SCMM.Steam.Shared.Requests
{
    public class SteamMarketHistoryPaginatedRequest : SteamPaginatedRequest
    {
        public Uri Uri => new Uri(
            $"{SteamConstants.SteamCommunityUrl}/market/myhistory?start={Start}&count={Count}&language={Uri.EscapeDataString(Language)}&currency={Uri.EscapeDataString(CurrencyId)}&norender={(NoRender ? "1" : "0")}"
        );
    }
}
