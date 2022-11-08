namespace SCMM.Steam.Data.Models.Community.Requests.Json
{
    public class SteamMarketItemOrdersHistogramJsonRequest : SteamRequest
    {
        public string ItemNameId { get; set; }

        public string Country { get; set; } = Constants.SteamCountryUS;

        public string Language { get; set; } = Constants.SteamLanguageEnglish;

        public string CurrencyId { get; set; }

        public bool NoRender { get; set; } = true;

        public override Uri Uri => new Uri(
            $"{Constants.SteamCommunityUrl}/market/itemordershistogram?country={Country}&language={Language}&currency={Uri.EscapeDataString(CurrencyId)}&item_nameid={Uri.EscapeDataString(ItemNameId)}&two_factor=0&norender={(NoRender ? "1" : "0")}"
        );
    }
}
