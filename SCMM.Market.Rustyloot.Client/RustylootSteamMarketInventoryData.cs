using System.Text.Json.Serialization;

namespace SCMM.Market.Rustyloot.Client;

public class RustylootSteamMarketInventoryData
{
    [JsonPropertyName("inventory")]
    public RustylootSteamMarketInventoryItem[] Inventory { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
