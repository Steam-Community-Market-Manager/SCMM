using Newtonsoft.Json;
using SCMM.Steam.Data.Models.WebApi.Models;

namespace SCMM.Steam.Data.Models.WebApi.Responses.IPublishedFileService
{
    public class QueryFilesJsonResponse
    {
        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("publishedfiledetails")]
        public List<PublishedFileDetails> PublishedFileDetails { get; set; }
    }
}
