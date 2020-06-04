using Newtonsoft.Json;

namespace SCMM.Steam.Shared.Models
{
    public class SteamMarketHistoryListing
    {
        [JsonProperty("listingid")]
        public string ListingId { get; set; }

        [JsonProperty("price")]
        public int Price { get; set; }

        [JsonProperty("fee")]
        public int Fee { get; set; }

        [JsonProperty("publisher_fee_app")]
        public int PublisherFeeApp { get; set; }

        [JsonProperty("publisher_fee_percent")]
        public string PublisherFeePercent { get; set; }

        [JsonProperty("currencyid")]
        public int CurrencyId { get; set; }

        [JsonProperty("asset")]
        public SteamMarketHistoryAsset Asset { get; set; }

        [JsonProperty("original_price")]
        public int OriginalPrice { get; set; }
    }
}
