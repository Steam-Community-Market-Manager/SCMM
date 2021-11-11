using SCMM.Steam.Data.Models.Extensions;
using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamMarketItemOrder
    {
        private string _priceText;
        private string _quantityText;

        [JsonIgnore]
        public long Price { get; private set; }

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
    }
}
