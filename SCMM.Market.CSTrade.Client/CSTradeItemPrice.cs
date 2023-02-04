using System.Text.Json.Serialization;

namespace SCMM.Market.CSTrade.Client
{
    public class CSTradeItemPrice
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("price")]
        public decimal? Price { get; set; }

        [JsonPropertyName("have")]
        public int Have { get; set; }

        [JsonPropertyName("tradable")]
        public int Tradable { get; set; }

        [JsonPropertyName("reservable")]
        public int Reservable { get; set; }

        [JsonPropertyName("can_take")]
        public int CanTake { get; set; }

        [JsonPropertyName("max")]
        public int Max { get; set; }
    }
}
