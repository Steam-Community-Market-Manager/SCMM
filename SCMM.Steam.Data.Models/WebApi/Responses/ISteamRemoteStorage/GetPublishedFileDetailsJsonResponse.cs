using SCMM.Steam.Data.Models.WebApi.Models;
using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.WebApi.Responses.ISteamRemoteStorage
{
    public class GetPublishedFileDetailsJsonResponse
    {
        [JsonPropertyName("result")]
        public int Result { get; set; }

        [JsonPropertyName("resultcount")]
        public int ResultCount { get; set; }

        [JsonPropertyName("publishedfiledetails")]
        public List<PublishedFileDetails> PublishedFileDetails { get; set; }
    }
}
