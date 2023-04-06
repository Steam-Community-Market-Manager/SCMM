using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.WebApi.Responses.IInventoryService
{
    public class GetItemDefMetaJsonResponse
    {
        [JsonPropertyName("modified")]
        public ulong Modified { get; set; }

        [JsonPropertyName("digest")]
        public string Digest { get; set; }
    }
}
