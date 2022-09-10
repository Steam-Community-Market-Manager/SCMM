using System.Text.Json.Serialization;

namespace SCMM.Market.Buff.Client
{
    public class BuffItemExtendedProductInfomation
    {
        [JsonPropertyName("localizedName")]
        public string LocalizedName { get; set; }
    }
}