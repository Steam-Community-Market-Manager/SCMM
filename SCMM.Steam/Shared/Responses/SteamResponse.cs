using Newtonsoft.Json;

namespace SCMM.Steam.Shared.Responses
{
    public class SteamResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}
