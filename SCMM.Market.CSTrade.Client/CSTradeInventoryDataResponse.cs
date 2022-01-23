using System.Text.Json.Serialization;

namespace SCMM.Market.CSTrade.Client
{
    public class CSTradeInventoryDataResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("inventory")]
        public IEnumerable<CSTradeItem> Inventory { get; set; }
    }
}
