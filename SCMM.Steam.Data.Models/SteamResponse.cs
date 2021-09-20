using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models
{
    public class SteamResponse<T>
    {
        [JsonPropertyName("response")]
        public T Response { get; set; }
    }
}
