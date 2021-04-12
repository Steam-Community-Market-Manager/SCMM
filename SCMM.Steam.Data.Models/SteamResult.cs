using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models
{
    public class SteamResult<T>
    {
        [JsonProperty("result")]
        public T Result { get; set; }
    }
}
