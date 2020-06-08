using System;

namespace SCMM.Steam.Shared.Requests.Community.Json
{
    public class SteamMarketSearchPaginatedJsonRequest : SteamPaginatedJsonRequest
    {
        public const int MaxPageSize = 100;

        public string AppId { get; set; }

        public bool GetDescriptions { get; set; }

        public bool SearchDescriptions { get; set; }

        public Uri Uri => new Uri(
            $"{SteamConstants.SteamCommunityUrl}/market/search/render/?appid={Uri.EscapeDataString(AppId)}&start={Start}&count={Count}&get_descriptions={(GetDescriptions ? "1" : "0")}&search_descriptions={(SearchDescriptions ? "1" : "0")}&language={Uri.EscapeDataString(Language)}&currency={Uri.EscapeDataString(CurrencyId)}&norender={(NoRender ? "1" : "0")}"
        );
    }
}
