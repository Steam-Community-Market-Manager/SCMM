using Newtonsoft.Json;

namespace SCMM.Steam.Shared.Responses.Community.Json
{
    public class SteamMarketPriceHistoryJsonResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("price_prefix")]
        public string PricePrefix { get; set; }

        [JsonProperty("price_suffix")]
        public string PriceSuffix { get; set; }

        /// <summary>
        /// x[0] = Timestamp, e.g. "May 15 2020 01: +0"
        /// x[1] = Price, e.g. "2.562"
        /// x[2] = Quantity, e.g. "46"
        /// </summary>
        [JsonProperty("prices")]
        public string[][] Prices { get; set; }
    }
}
