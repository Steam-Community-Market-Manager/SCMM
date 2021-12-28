using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.WebApi.Responses.IInventoryService
{
    public class CombineItemStacksJsonResponse
    {
        [JsonPropertyName("item_json")]
        public string ItemJson { get; set; }
    }
}
