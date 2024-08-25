using System.Text.Json.Serialization;

namespace SCMM.Market.RapidSkins.Client;

public class RapidSkinsGraphQLRequest
{
    [JsonPropertyName("operationName")]
    public string OperationName { get; set; }

    [JsonPropertyName("query")]
    public string Query { get; set; }

    [JsonPropertyName("variables")]
    public dynamic Variables { get; set; }

}
