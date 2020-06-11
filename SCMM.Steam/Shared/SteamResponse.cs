using Newtonsoft.Json;

namespace SCMM.Steam.Shared.WebAPI.ISteamEconomy.GetAssetPrices
{
    public class SteamResponse<T>
    {
        [JsonProperty("response")]
        public T Response { get; set; }
    }
}
