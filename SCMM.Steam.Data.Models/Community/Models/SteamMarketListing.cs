using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamMarketListing
    {
        [JsonPropertyName("listingid")]
        public string ListingId { get; set; }

        [JsonPropertyName("time_created")]
        public long TimeCreated { get; set; }

        [JsonPropertyName("steamid_lister")]
        public string SteamIdLister { get; set; }

        [JsonPropertyName("asset")]
        public SteamMarketAsset Asset { get; set; }

        [JsonPropertyName("original_amount_listed")]
        public int OriginalAmountListed { get; set; }

        [JsonPropertyName("original_price")]
        public int OriginalPrice { get; set; }

        [JsonPropertyName("price")]
        public int Price { get; set; }

        [JsonPropertyName("fee")]
        public int Fee { get; set; }

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
    }
}
