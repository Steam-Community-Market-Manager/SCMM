using Newtonsoft.Json;
using System;

namespace SCMM.Steam.Data.Models.Workshop.Requests
{
    public class SteamWorkshopDownloaderStatusJsonRequest
    {
        [JsonProperty("uuids")]
        public Guid[] Uuids { get; set; }
    }
}
