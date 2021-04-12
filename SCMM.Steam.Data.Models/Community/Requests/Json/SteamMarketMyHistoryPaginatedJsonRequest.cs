using System;

namespace SCMM.Steam.Data.Models.Community.Requests.Json
{
    public class SteamMarketMyHistoryPaginatedJsonRequest : SteamPaginatedJsonRequest
    {
        public const int MaxPageSize = 500;

        public override Uri Uri => new Uri(
            $"{Constants.SteamCommunityUrl}/market/myhistory?start={Start}&count={Count}&language={Uri.EscapeDataString(Language)}&currency={Uri.EscapeDataString(CurrencyId)}&norender={(NoRender ? "1" : "0")}"
        );
    }
}
