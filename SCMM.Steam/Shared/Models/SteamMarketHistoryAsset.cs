using Newtonsoft.Json;

namespace SCMM.Steam.Shared.Models
{
    public class SteamMarketHistoryAsset
    {
        [JsonProperty("currency")]
        public int Currency { get; set; }

        [JsonProperty("appid")]
        public int AppId { get; set; }

        [JsonProperty("contextid")]
        public string ContextId { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("classid")]
        public string ClassId { get; set; }

        [JsonProperty("instanceid")]
        public string InstanceId { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("new_id")]
        public string NewId { get; set; }

        [JsonProperty("new_contextid")]
        public string NewContextId { get; set; }
    }
}
