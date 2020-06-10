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
                var priceGroups = Regex.Match(_priceText, @"([\d\.]+)").Groups;
                var priceNumeric = (priceGroups.Count > 1) ? priceGroups[1].Value : "0";
                Price = Int32.Parse(priceNumeric.Replace(".", "").Replace(",", ""));
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
                Quantity = Int32.Parse(value?.Replace(",", "") ?? "0");
            }
        }
    }
}
