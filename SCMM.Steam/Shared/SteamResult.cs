using Newtonsoft.Json;

namespace SCMM.Steam.Shared.WebAPI.ISteamEconomy.GetAssetPrices
{
    public class SteamResult<T>
    {
        [JsonProperty("result")]
        public T Result { get; set; }
    }
}
