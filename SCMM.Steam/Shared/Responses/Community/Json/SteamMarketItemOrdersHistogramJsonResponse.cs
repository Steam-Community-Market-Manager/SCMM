using Newtonsoft.Json;
using SCMM.Steam.Shared.Models;

namespace SCMM.Steam.Shared.Responses.Community.Json
{
    public class SteamMarketItemOrdersHistogramJsonResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("price_prefix")]
        public string PricePrefix { get; set; }

        [JsonProperty("price_suffix")]
        public string PriceSuffix { get; set; }

        [JsonProperty("buy_order_table")]
        public SteamMarketItemOrder[] BuyOrderTable { get; set; }

        [JsonProperty("sell_order_table")]
        public SteamMarketItemOrder[] SellOrderTable { get; set; }

        /// <summary>
        /// x[0] = Price, e.g. "5.32"
        /// x[1] = Quantity, e.g. 5
        /// x[2] = Description, e.g. "5 buy orders at NZ$ 5.32 or higher"
        /// </summary>
        [JsonProperty("buy_order_graph")]
        public string[][] BuyOrderGraph { get; set; }

        /// <summary>
        /// x[0] = Price, e.g. "5.32"
        /// x[1] = Quantity, e.g. 5
        /// x[2] = Description, e.g. "5 sell orders at NZ$ 5.32 or lower"
        /// </summary>
        [JsonProperty("sell_order_graph")]
        public string[][] SellOrderGraph { get; set; }

        public decimal GraphMinY => 0;

        [JsonProperty("graph_max_y")]
        public decimal GraphMaxY { get; set; }

        [JsonProperty("graph_min_x")]
        public decimal GraphMinX { get; set; }

        [JsonProperty("graph_max_x")]
        public decimal GraphMaxX { get; set; }
    }
}
