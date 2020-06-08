using Newtonsoft.Json;

namespace SCMM.Steam.Shared.Models
{
    public class SteamMarketEvent
    {
        [JsonProperty("listingid")]
        public string ListingId { get; set; }

        [JsonProperty("purchaseid")]
        public string PurchaseId { get; set; }

        [JsonProperty("event_type")]
        public SteamMarketEventType EventType { get; set; }

        [JsonProperty("time_event")]
        public long TimeEvent { get; set; }

        [JsonProperty("time_event_fraction")]
        public long TimeEventFraction { get; set; }

        [JsonProperty("steamid_actor")]
        public string SteamIdActor { get; set; }

        [JsonProperty("date_event")]
        public string DateEvent { get; set; }
    }
}
