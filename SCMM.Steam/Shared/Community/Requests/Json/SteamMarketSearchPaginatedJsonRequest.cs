using System;

namespace SCMM.Steam.Shared.Community.Requests.Json
{
    public class SteamMarketSearchPaginatedJsonRequest : SteamPaginatedJsonRequest
    {
        public const int MaxPageSize = 100;

        public const string SortColumnDefault = "default";
        public const string SortColumnName = "name";
        public const string SortColumnQuantity = "quantity";
        public const string SortColumnPrice = "price";
        public const string SortDirectionAscending = "asc";
        public const string SortDirectionDescending = "desc";

        public string AppId { get; set; }

        public bool GetDescriptions { get; set; }

        public bool SearchDescriptions { get; set; }

        public string SortColumn { get; set; } = SortColumnDefault;

        public string SortDirection { get; set; } = SortDirectionAscending;

        public override Uri Uri => new Uri(
            $"{SteamConstants.SteamCommunityUrl}/market/search/render/?appid={Uri.EscapeDataString(AppId)}&start={Start}&count={Count}&get_descriptions={(GetDescriptions ? "1" : "0")}&search_descriptions={(SearchDescriptions ? "1" : "0")}&sort_column={Uri.EscapeDataString(SortColumn)}&sort_dir={Uri.EscapeDataString(SortDirection)}&language={Uri.EscapeDataString(Language)}&currency={Uri.EscapeDataString(CurrencyId)}&norender={(NoRender ? "1" : "0")}"
        );
    }
}
