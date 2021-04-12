using Newtonsoft.Json;
using SCMM.Steam.Data.Models.Extensions;

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
                Price = value.SteamPriceAsInt();
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
                Quantity = value.SteamQuantityValueAsInt();
            }
        }
    }
}
