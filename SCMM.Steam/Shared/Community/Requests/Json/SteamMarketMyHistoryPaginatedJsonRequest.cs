using System;

namespace SCMM.Steam.Shared.Community.Requests.Json
{
    public class SteamMarketMyHistoryPaginatedJsonRequest : SteamPaginatedJsonRequest
    {
        public const int MaxPageSize = 500;

        public override Uri Uri => new Uri(
            $"{SteamConstants.SteamCommunityUrl}/market/myhistory?start={Start}&count={Count}&language={Uri.EscapeDataString(Language)}&currency={Uri.EscapeDataString(CurrencyId)}&norender={(NoRender ? "1" : "0")}"
        );
    }
}
