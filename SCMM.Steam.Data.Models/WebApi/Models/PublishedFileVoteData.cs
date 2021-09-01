using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models.WebApi.Models
{
    public class PublishedFileVoteData
    {
        [JsonProperty("score")]
        public decimal Score { get; set; }

        [JsonProperty("votes_up")]
        public uint VotesUp { get; set; }

        [JsonProperty("votes_down")]
        public uint VotesDown { get; set; }
    }
}
