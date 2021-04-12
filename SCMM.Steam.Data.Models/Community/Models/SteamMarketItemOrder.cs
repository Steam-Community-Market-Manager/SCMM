using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamMarketItemOrder
    {
        private string _priceText;
        private string _quantityText;

        public long Price { get; private set; }

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
    }
}
