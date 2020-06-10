using System;

namespace SCMM.Steam.Shared.WebAPI.ISteamEconomy.GetAssetPrices
{
    public class SteamEconomyGetAssetPricesRequest : SteamRequest
    {
        public string ApplicationKey { get; set; }

        public string AppId { get; set; }

        public string Language { get; set; }

        public string Currency { get; set; }

        public Uri Uri => new Uri(
            $"{SteamConstants.SteamWebApiUrl}/ISteamEconomy/GetAssetPrices/v1/?appid={Uri.EscapeDataString(AppId)}&key={Uri.EscapeDataString(ApplicationKey)}&language={Uri.EscapeDataString(Language)}&currency={Uri.EscapeDataString(Currency)}"
        );
    }
}
