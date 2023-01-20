namespace SCMM.Steam.Data.Models.Community.Requests.Json
{
    public class SteamMarketPriceOverviewJsonRequest : SteamRequest
    {
        public string AppId { get; set; }

        public string MarketHashName { get; set; }

        public string Country { get; set; } = Constants.SteamDefaultCountry;

        public string Language { get; set; } = Constants.SteamDefaultLanguage;

        public string CurrencyId { get; set; } = Constants.SteamDefaultCurrencyId.ToString();

        public bool NoRender { get; set; } = true;

        public override Uri Uri => new Uri(
            $"{Constants.SteamCommunityUrl}/market/priceoverview?appid={Uri.EscapeDataString(AppId ?? String.Empty)}&market_hash_name={Uri.EscapeDataString(MarketHashName ?? String.Empty)}&country={Uri.EscapeDataString(Country ?? String.Empty)}&language={Uri.EscapeDataString(Language ?? String.Empty)}&currency={Uri.EscapeDataString(CurrencyId ?? String.Empty)}&norender={(NoRender ? "1" : "0")}"
        );
    }
}
