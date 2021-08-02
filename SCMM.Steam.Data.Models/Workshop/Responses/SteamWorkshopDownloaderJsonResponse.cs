using Newtonsoft.Json;
using System;

namespace SCMM.Steam.Data.Models.Workshop.Responses
{
    public class SteamWorkshopDownloaderJsonResponse
    {
        [JsonProperty("uuid")]
        public Guid Uuid { get; set; }
    }
}
