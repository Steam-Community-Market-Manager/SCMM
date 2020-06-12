using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;

namespace SCMM.Steam.Shared.Community.Models
{
    public class SteamMarketItemOrder
    {
        private string _priceText;
        private string _quantityText;

        public int Price { get; private set; }

        [JsonProperty("price")]
        public string PriceText
        {
            get => _priceText;
            set
            {
                _priceText = value;
                Price = SteamEconomyHelper.GetPriceValueAsInt(value);
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
                Quantity = SteamEconomyHelper.GetQuantityValueAsInt(value);
            }
        }
    }
}
