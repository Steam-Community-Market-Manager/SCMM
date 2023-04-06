namespace SCMM.Steam.Data.Models.WebApi.Requests.ISteamEconomy
{
    /// <summary>
    /// https://steamapi.xpaw.me/#ISteamEconomy/GetAssetPrices
    /// </summary>
    public class GetAssetPricesJsonRequest : SteamRequest
    {
        public string Key { get; set; }

        public ulong AppId { get; set; }

        public string Currency { get; set; }

        public string Language { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamWebApiUrl}/ISteamEconomy/GetAssetPrices/v1/?key={Uri.EscapeDataString(Key)}&appId={AppId}&currency={Uri.EscapeDataString(Currency ?? String.Empty)}&language={Uri.EscapeDataString(Language ?? String.Empty)}"
        );
    }
}
