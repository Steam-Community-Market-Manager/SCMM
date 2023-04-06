using SCMM.Steam.Data.Models.WebApi.Models;
using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.WebApi.Responses.ISteamEconomy
{
    public class GetAssetPricesJsonResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("assets")]
        public List<AssetPrice> Assets { get; set; }
    }
}
