using System.Text.Json.Serialization;

namespace SCMM.Market.RapidSkins.Client;
public class RapidSkinsPaginatedItems
{
    [JsonPropertyName("lastPage")]
    public bool LastPage { get; set; }

    [JsonPropertyName("items")]
    public RapidSkinsItem[] Items { get; set; }
}
