using System.Text.Json.Serialization;

namespace SCMM.Market.Rustyloot.Client;

public class RustylootResponse
{
    [JsonPropertyName("error")]
    public bool Error { get; set; }
}
