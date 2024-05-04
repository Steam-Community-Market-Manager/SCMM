using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SCMM.Market.Rustyloot.Client;

public class RustylootSteamMarketResponse : RustylootResponse
{
    [JsonPropertyName("data")]
    public RustylootSteamMarketInventoryData Data { get; set; }
}
