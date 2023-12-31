﻿using SCMM.Steam.Data.Models.Community.Models;
using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Community.Responses.Json
{
    public class SteamMarketItemOrdersHistogramJsonResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("price_prefix")]
        public string PricePrefix { get; set; }

        [JsonPropertyName("price_suffix")]
        public string PriceSuffix { get; set; }

        /// <summary>
        /// This is the true sell order count, sell list may be less as Steam caps results to 100
        /// </summary>
        [JsonPropertyName("sell_order_count")]
        public string SellOrderCount { get; set; }

        /// <summary>
        /// Steam only returns the top 5 results
        /// </summary>
        [JsonPropertyName("sell_order_table")]
        public SteamMarketItemOrder[] SellOrderTable { get; set; }

        /// <summary>
        /// Steam only returns the top 100 results
        /// x[0] = Price, e.g. "5.32"
        /// x[1] = Quantity, e.g. 5
        /// x[2] = Description, e.g. "5 sell orders at NZ$ 5.32 or lower"
        /// </summary>
        [JsonPropertyName("sell_order_graph")]
        public string[][] SellOrderGraph { get; set; }

        /// <summary>
        /// This is the true buy order count, buy list may be less as Steam caps results to 100
        /// </summary>
        [JsonPropertyName("buy_order_count")]
        public string BuyOrderCount { get; set; }

        /// <summary>
        /// Steam only returns the top 5 results
        /// </summary>
        [JsonPropertyName("buy_order_table")]
        public SteamMarketItemOrder[] BuyOrderTable { get; set; }

        /// <summary>
        /// Steam only returns the top 100 results
        /// x[0] = Price, e.g. "5.32"
        /// x[1] = Quantity, e.g. 5
        /// x[2] = Description, e.g. "5 buy orders at NZ$ 5.32 or higher"
        /// </summary>
        [JsonPropertyName("buy_order_graph")]
        public string[][] BuyOrderGraph { get; set; }

        public decimal GraphMinY => 0;

        [JsonPropertyName("graph_max_y")]
        public decimal GraphMaxY { get; set; }

        [JsonPropertyName("graph_min_x")]
        public decimal GraphMinX { get; set; }

        [JsonPropertyName("graph_max_x")]
        public decimal GraphMaxX { get; set; }
    }
}
