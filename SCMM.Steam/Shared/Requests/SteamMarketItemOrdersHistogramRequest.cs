using System;

namespace SCMM.Steam.Shared.Requests
{
    public class SteamMarketItemOrdersHistogramRequest : SteamRequest
    {
        public string ItemNameId { get; set; }

        public string Language { get; set; }

        public string CurrencyId { get; set; }

        public bool NoRender { get; set; } = true;

        public Uri Uri => new Uri(
            $"{SteamConstants.SteamCommunityUrl}/market/itemordershistogram?item_nameid={Uri.EscapeDataString(ItemNameId)}&language={Language}&currency={Uri.EscapeDataString(CurrencyId)}&norender={(NoRender ? "1" : "0")}"
        );
    }
}
