using Newtonsoft.Json;
using System.Collections.Generic;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamAssetClassDescription
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
