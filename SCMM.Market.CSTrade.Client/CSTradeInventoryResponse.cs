﻿using System.Text.Json.Serialization;

namespace SCMM.Market.CSTrade.Client
{
    public class CSTradeInventoryResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("inventory")]
        public IEnumerable<CSTradeInventoryItem> Inventory { get; set; }
    }
}
