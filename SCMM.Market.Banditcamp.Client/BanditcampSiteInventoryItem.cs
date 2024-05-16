using System.Text.Json.Serialization;

namespace SCMM.Market.Banditcamp.Client;

public class BanditcampSiteInventoryItem
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("inStock")]
    public int InStock { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("price")]
    public long Price { get; set; }
}
