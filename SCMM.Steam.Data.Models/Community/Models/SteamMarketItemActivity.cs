using System.Text.Json.Serialization;
using SCMM.Steam.Data.Models.Extensions;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamMarketItemActivity
    {
        /// <summary>
        /// SellOrder, BuyOrder
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonIgnore]
        public long Price { get; private set; }

        private string _priceText;
        [JsonPropertyName("price")]
        public string PriceText
        {
            get => _priceText;
            set
            {
                _priceText = value;
                Price = value.SteamPriceAsInt();
            }
        }

        [JsonIgnore]
        public int Quantity { get; private set; }

        private string _quantityText;
        [JsonPropertyName("quantity")]
        public string QuantityText
        {
            get => _quantityText;
            set
            {
                _quantityText = value;
                Quantity = value.SteamQuantityValueAsInt();
            }
        }

        [JsonPropertyName("time")]
        public long Time { get; set; }

        [JsonPropertyName("avatar_buyer")]
        public string AvatarBuyer { get; set; }

        [JsonPropertyName("avatar_medium_buyer")]
        public string AvatarMediumBuyer { get; set; }

        [JsonPropertyName("persona_buyer")]
        public string PersonaBuyer { get; set; }

        [JsonPropertyName("avatar_seller")]
        public string AvatarSeller { get; set; }

        [JsonPropertyName("avatar_medium_seller")]
        public string AvatarMediumSeller { get; set; }

        [JsonPropertyName("persona_seller")]
        public string PersonaSeller { get; set; }

    }
}
