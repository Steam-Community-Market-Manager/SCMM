using System.Text.Json.Serialization;

namespace SCMM.Market.iTradegg.Client
{
    public class iTradeggInventoryResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("game")]
        public string Game { get; set; }

        [JsonPropertyName("inventory")]
        public iTradeggInventoryItemsResponse Inventory { get; set; }
    }
}
