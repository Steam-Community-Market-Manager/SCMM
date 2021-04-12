using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models
{
    public class SteamResponse<T>
    {
        [JsonProperty("response")]
        public T Response { get; set; }
    }
}
