using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamMarketPurchase
    {
        [JsonProperty("purchaseid")]
        public string PurchaseId { get; set; }

        [JsonProperty("listingid")]
        public string ListingId { get; set; }

        [JsonProperty("time_sold")]
        public long TimeSold { get; set; }

        [JsonProperty("steamid_purchaser")]
        public string SteamIdPurchaser { get; set; }

        [JsonProperty("needs_rollback")]
        public int NeedsRollback { get; set; }

        [JsonProperty("failed")]
        public int Failed { get; set; }

        [JsonProperty("asset")]
        public SteamMarketAsset Asset { get; set; }

        [JsonProperty("paid_amount")]
        public int PaidAmount { get; set; }

        [JsonProperty("paid_fee")]
        public int PaidFee { get; set; }

        [JsonProperty("currencyid")]
        public string CurrencyId { get; set; }

        [JsonProperty("steam_fee")]
        public int SteamFee { get; set; }

        [JsonProperty("publisher_fee")]
        public int PublisherFee { get; set; }

        [JsonProperty("publisher_fee_percent")]
        public string PublisherFeePercent { get; set; }

        [JsonProperty("publisher_fee_app")]
        public int PublisherFeeApp { get; set; }

        [JsonProperty("received_amount")]
        public int ReceivedAmount { get; set; }

        [JsonProperty("received_currencyid")]
        public string ReceivedCurrencyId { get; set; }

        [JsonProperty("funds_returned")]
        public int FundsReturned { get; set; }

        [JsonProperty("avatar_actor")]
        public string AvatarActor { get; set; }

        [JsonProperty("persona_actor")]
        public string PersonaActor { get; set; }
    }
}
