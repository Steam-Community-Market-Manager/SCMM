using System.Text.Json.Serialization;
using SCMM.Steam.Data.Models.WebApi.Models;

namespace SCMM.Steam.Data.Models.WebApi.Responses.IPublishedFileService
{
    public class QueryFilesJsonResponse
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("publishedfiledetails")]
        public List<PublishedFileDetails> PublishedFileDetails { get; set; }
    }
}
