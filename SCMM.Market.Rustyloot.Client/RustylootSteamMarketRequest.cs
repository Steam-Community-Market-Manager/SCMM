using System.Text.Json.Serialization;

namespace SCMM.Market.Rustyloot.Client;

public class RustylootSteamMarketRequest
{
    [JsonPropertyName("search")]
    public string Search { get; set; } = String.Empty;

    [JsonPropertyName("asc")]
    public bool Asc { get; set; } = false;

    [JsonPropertyName("offset")]
    public int Offset { get; set; } = 0;

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 9999;
}
