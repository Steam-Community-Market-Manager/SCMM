using System.Text.Json.Serialization;

namespace SCMM.Market.SkinsMonkey.Client
{
    public class SkinsMonkeyInventoryResponse
    {
        [JsonPropertyName("assets")]
        public IEnumerable<SkinsMonkeyItemListing> Assets { get; set; }
    }
}
