using Newtonsoft.Json;

namespace SCMM.Steam.Shared
{
    public class SteamResponse<T>
    {
        [JsonProperty("response")]
        public T Response { get; set; }
    }
}
