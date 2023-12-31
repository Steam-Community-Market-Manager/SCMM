﻿using System.Text.Json.Serialization;

namespace SCMM.Market.SkinSwap.Client
{
    public class SkinSwapItem
    {
        [JsonPropertyName("appid")]
        public long AppId { get; set; }

        [JsonPropertyName("market_hash_name")]
        public string MarketHashName { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }
    }
}
