using System.Text.Json.Serialization;

namespace SCMM.Market.SkinBaron.Client
{
    public class SkinBaronItemOffer
    {
        [JsonPropertyName("appId")]
        public ulong AppId { get; set; }

        [JsonPropertyName("extendedProductInformation")]
        public SkinBaronItemExtendedProductInfomation ExtendedProductInformation { get; set; }

        [JsonPropertyName("id")]
        public ulong Id { get; set; }

        [JsonPropertyName("lowestPrice")]
        public decimal LowestPrice { get; set; }

        [JsonPropertyName("numberOfOffers")]
        public decimal NumberOfOffers { get; set; }

        [JsonPropertyName("numberOfOffersTradeLocked")]
        public decimal NumberOfOffersTradeLocked { get; set; }

        [JsonPropertyName("steamMarketPrice")]
        public decimal SteamMarketPrice { get; set; }
    }
}