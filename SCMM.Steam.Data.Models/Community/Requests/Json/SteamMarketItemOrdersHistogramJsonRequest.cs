namespace SCMM.Steam.Data.Models.Community.Requests.Json
{
    public class SteamMarketItemOrdersHistogramJsonRequest : SteamRequest
    {
        public string ItemNameId { get; set; }

        public string Language { get; set; }

        public string CurrencyId { get; set; }

        public bool NoRender { get; set; } = true;

        public override Uri Uri => new Uri(
            $"{Constants.SteamCommunityUrl}/market/itemordershistogram?item_nameid={Uri.EscapeDataString(ItemNameId)}&language={Language}&currency={Uri.EscapeDataString(CurrencyId)}&norender={(NoRender ? "1" : "0")}"
        );
    }
}
