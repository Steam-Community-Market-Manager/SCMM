using System.Text.Json.Serialization;

namespace SCMM.Market.Rustyloot.Client;

public class RustylootSteamMarketInventoryItem
{
    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonPropertyName("assetid")]
    public string AssetId { get; set; }

    [JsonPropertyName("flagged")]
    public int Flagged { get; set; }

    [JsonPropertyName("locked")]
    public int Locked { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("price")]
    public long Price { get; set; }
}
