﻿using System.Text.Json.Serialization;

namespace SCMM.Market.SwapGG.Client
{
    public class SwapGGTradeItem
    {
        [JsonPropertyName("g")]
        public long AppId { get; set; }

        [JsonPropertyName("n")]
        public string Name { get; set; }

        [JsonPropertyName("a")]
        public string[] ItemIds { get; set; }

        [JsonPropertyName("p")]
        public long Price { get; set; }

        [JsonPropertyName("i")]
        public string Icon { get; set; }

        [JsonPropertyName("m")]
        public IDictionary<string, string> Tags { get; set; }
    }
}