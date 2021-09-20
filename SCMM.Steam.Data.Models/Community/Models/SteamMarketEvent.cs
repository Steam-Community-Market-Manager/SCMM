using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamMarketEvent
    {
        [JsonPropertyName("listingid")]
        public string ListingId { get; set; }

        [JsonPropertyName("purchaseid")]
        public string PurchaseId { get; set; }

        [JsonPropertyName("event_type")]
        public SteamMarketEventType EventType { get; set; }

        [JsonPropertyName("time_event")]
        public long TimeEvent { get; set; }

        [JsonPropertyName("time_event_fraction")]
        public long TimeEventFraction { get; set; }

        [JsonPropertyName("steamid_actor")]
        public string SteamIdActor { get; set; }

        [JsonPropertyName("date_event")]
        public string DateEvent { get; set; }
    }
}
