using Newtonsoft.Json;

namespace SCMM.Steam.Shared.WebAPI.ISteamEconomy.GetAssetPrices
{
    public class GetAssetPricesResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}
