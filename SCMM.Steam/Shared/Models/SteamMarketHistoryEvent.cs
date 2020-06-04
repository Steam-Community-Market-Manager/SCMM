using Newtonsoft.Json;

namespace SCMM.Steam.Shared.Models
{
    public class SteamMarketHistoryEvent
    {
        [JsonProperty("listingid")]
        public string ListingId { get; set; }

        [JsonProperty("purchaseid")]
        public string PurchaseId { get; set; }

        [JsonProperty("event_type")]
        public SteamMarketHistoryEventType EventType { get; set; }

        [JsonProperty("time_event")]
        public int TimeEvent { get; set; }

        [JsonProperty("time_event_fraction")]
        public int TimeEventFraction { get; set; }

        [JsonProperty("steamid_actor")]
        public string SteamIdActor { get; set; }

        [JsonProperty("date_event")]
        public string DateEvent { get; set; }
    }
}
