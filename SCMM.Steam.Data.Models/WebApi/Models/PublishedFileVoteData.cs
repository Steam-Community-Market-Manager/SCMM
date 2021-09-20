using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.WebApi.Models
{
    public class PublishedFileVoteData
    {
        [JsonPropertyName("score")]
        public decimal Score { get; set; }

        [JsonPropertyName("votes_up")]
        public uint VotesUp { get; set; }

        [JsonPropertyName("votes_down")]
        public uint VotesDown { get; set; }
    }
}
