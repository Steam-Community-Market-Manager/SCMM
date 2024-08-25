using System.Text.Json.Serialization;

namespace SCMM.Market.RapidSkins.Client;

public class RapidSkinsGraphQLResponse<T>
{
    [JsonPropertyName("data")]
    public T Data { get; set; }
}
