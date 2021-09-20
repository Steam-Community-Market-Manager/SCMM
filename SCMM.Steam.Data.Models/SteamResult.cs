using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models
{
    public class SteamResult<T>
    {
        [JsonPropertyName("result")]
        public T Result { get; set; }
    }
}
