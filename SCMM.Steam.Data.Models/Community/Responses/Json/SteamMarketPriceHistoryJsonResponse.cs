using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Community.Responses.Json
{
    public class SteamMarketPriceHistoryJsonResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("price_prefix")]
        public string PricePrefix { get; set; }

        [JsonPropertyName("price_suffix")]
        public string PriceSuffix { get; set; }

        /// <summary>
        /// x[0] = Timestamp, e.g. "May 15 2020 01: +0"
        /// x[1] = Price, e.g. "2.562"
        /// x[2] = Quantity, e.g. "46"
        /// </summary>
        [JsonPropertyName("prices")]
        public string[][] Prices { get; set; }
    }
}
