using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamMarketPurchase
    {
        [JsonPropertyName("purchaseid")]
        public string PurchaseId { get; set; }

        [JsonPropertyName("listingid")]
        public string ListingId { get; set; }

        [JsonPropertyName("time_sold")]
        public long TimeSold { get; set; }

        [JsonPropertyName("steamid_purchaser")]
        public string SteamIdPurchaser { get; set; }

        [JsonPropertyName("needs_rollback")]
        public int NeedsRollback { get; set; }

        [JsonPropertyName("failed")]
        public int Failed { get; set; }

        [JsonPropertyName("asset")]
        public SteamMarketAsset Asset { get; set; }

        [JsonPropertyName("paid_amount")]
        public int PaidAmount { get; set; }

        [JsonPropertyName("paid_fee")]
        public int PaidFee { get; set; }

        [JsonPropertyName("currencyid")]
        public string CurrencyId { get; set; }

        [JsonPropertyName("steam_fee")]
        public int SteamFee { get; set; }

        [JsonPropertyName("publisher_fee")]
        public int PublisherFee { get; set; }

        [JsonPropertyName("publisher_fee_percent")]
        public string PublisherFeePercent { get; set; }

        [JsonPropertyName("publisher_fee_app")]
        public int PublisherFeeApp { get; set; }

        [JsonPropertyName("received_amount")]
        public int ReceivedAmount { get; set; }

        [JsonPropertyName("received_currencyid")]
        public string ReceivedCurrencyId { get; set; }

        [JsonPropertyName("funds_returned")]
        public int FundsReturned { get; set; }

        [JsonPropertyName("avatar_actor")]
        public string AvatarActor { get; set; }

        [JsonPropertyName("persona_actor")]
        public string PersonaActor { get; set; }
    }
}
