﻿using System.Text.Json.Serialization;

namespace SCMM.Market.LootFarm.Client
{
    public class LootFarmItemPrice
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("price")]
        public long Price { get; set; }

        [JsonPropertyName("have")]
        public int Have { get; set; }

        [JsonPropertyName("max")]
        public int Max { get; set; }

        [JsonPropertyName("rate")]
        public string Rate { get; set; }

        [JsonPropertyName("tr")]
        public int? Tradable { get; set; }

        [JsonPropertyName("res")]
        public int? Reservable { get; set; }
    }
}
