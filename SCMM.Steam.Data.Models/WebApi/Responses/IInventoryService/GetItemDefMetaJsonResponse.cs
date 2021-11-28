using SCMM.Steam.Data.Models.WebApi.Models;
using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.WebApi.Responses.IInventoryService
{
    public class GetItemDefMetaJsonResponse
    {
        [JsonPropertyName("modified")]
        public long Modified { get; set; }

        [JsonPropertyName("digest")]
        public string Digest { get; set; }
    }
}
