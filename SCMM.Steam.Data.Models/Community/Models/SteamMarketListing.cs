using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamMarketListing
    {
        [JsonProperty("listingid")]
        public string ListingId { get; set; }

        [JsonProperty("time_created")]
        public long TimeCreated { get; set; }

        [JsonProperty("steamid_lister")]
        public string SteamIdLister { get; set; }

        [JsonProperty("asset")]
        public SteamMarketAsset Asset { get; set; }

        [JsonProperty("original_amount_listed")]
        public int OriginalAmountListed { get; set; }

        [JsonProperty("original_price")]
        public int OriginalPrice { get; set; }

        [JsonProperty("price")]
        public int Price { get; set; }

        [JsonProperty("fee")]
        public int Fee { get; set; }

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
    }
}
