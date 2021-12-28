using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.WebApi.Models
{
    public class ItemStackModificationOutcome
    {
        [JsonPropertyName("accountid")]
        public string AccountId { get; set; }

        [JsonPropertyName("itemid")]
        public string ItemId { get; set; }

        [JsonPropertyName("quantity")]
        public uint Quantity { get; set; }

        [JsonPropertyName("originalitemid")]
        public string OriginalItemId { get; set; }

        [JsonPropertyName("itemdefid")]
        public string ItemDefId { get; set; }

        [JsonPropertyName("appid")]
        public ulong AppId { get; set; }

        [JsonPropertyName("acquired")]
        public string Acquired { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("origin")]
        public string Origin { get; set; }

        [JsonPropertyName("state_changed_timestamp")]
        public string StateChangedTimestamp { get; set; }
    }
}
