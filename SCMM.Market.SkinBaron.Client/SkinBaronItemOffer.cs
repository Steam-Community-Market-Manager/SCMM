using System.Text.Json.Serialization;

namespace SCMM.Market.SkinBaron.Client
{
    public class SkinBaronItemOffer
    {
        [JsonPropertyName("appId")]
        public ulong AppId { get; set; }

        [JsonPropertyName("extendedProductInformation")]
        public SkinBaronItemExtendedProductInformation ExtendedProductInformation { get; set; }

        [JsonPropertyName("id")]
        public ulong Id { get; set; }

        [JsonPropertyName("lowestPrice")]
        public decimal LowestPrice { get; set; }

        [JsonPropertyName("numberOfOffers")]
        public int NumberOfOffers { get; set; }

        [JsonPropertyName("numberOfOffersTradeLocked")]
        public int NumberOfOffersTradeLocked { get; set; }

        [JsonPropertyName("steamMarketPrice")]
        public decimal SteamMarketPrice { get; set; }
    }
}