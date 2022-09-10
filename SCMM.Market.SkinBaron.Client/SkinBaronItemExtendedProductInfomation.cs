using System.Text.Json.Serialization;

namespace SCMM.Market.SkinBaron.Client
{
    public class SkinBaronItemExtendedProductInfomation
    {
        [JsonPropertyName("localizedName")]
        public string LocalizedName { get; set; }
    }
}