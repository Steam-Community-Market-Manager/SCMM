﻿using System.Text.Json.Serialization;

namespace SCMM.Market.DMarket.Client
{
    public class DMarketItem
    {
        [JsonPropertyName("itemId")]
        public Guid ItemId { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("classId")]
        public string ClassId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("price")]
        public IDictionary<string, long> Price { get; set; }

        [JsonPropertyName("suggestedPrice")]
        public IDictionary<string, long> SuggestedPrice { get; set; }

    }
}