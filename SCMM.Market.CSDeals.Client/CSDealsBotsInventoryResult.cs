using System.Text.Json.Serialization;

namespace SCMM.Market.CSDeals.Client
{
    public class CSDealsBotsInventoryResult
    {
        [JsonPropertyName("items")]
        public CSDealsAppItems Items { get; set; }
    }
}
