using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamMarketItemActivity
    {
        /// <summary>
        /// SellOrder, BuyOrder
        /// </summary>
        [JsonProperty("type")]
        public string PurchaseId { get; set; }

        public long Price { get; private set; }

        private string _priceText;
        [JsonProperty("price")]
        public string PriceText
        {
            get => _priceText;
            set
            {
                _priceText = value;
                Price = EconomyExtensions.SteamPriceAsInt(value);
            }
        }

        public int Quantity { get; private set; }

        private string _quantityText;
        [JsonProperty("quantity")]
        public string QuantityText
        {
            get => _quantityText;
            set
            {
                _quantityText = value;
                Quantity = EconomyExtensions.SteamQuantityValueAsInt(value);
            }
        }

        [JsonProperty("time")]
        public long Time { get; set; }

        [JsonProperty("avatar_buyer")]
        public string AvatarBuyer { get; set; }

        [JsonProperty("avatar_medium_buyer")]
        public string AvatarMediumBuyer { get; set; }

        [JsonProperty("persona_buyer")]
        public string PersonaBuyer { get; set; }

        [JsonProperty("avatar_seller")]
        public string AvatarSeller { get; set; }

        [JsonProperty("avatar_medium_seller")]
        public string AvatarMediumSeller { get; set; }

        [JsonProperty("persona_seller")]
        public string PersonaSeller { get; set; }

    }
}
