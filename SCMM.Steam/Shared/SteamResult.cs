using Newtonsoft.Json;

namespace SCMM.Steam.Shared
{
    public class SteamResult<T>
    {
        [JsonProperty("result")]
        public T Result { get; set; }
    }
}
