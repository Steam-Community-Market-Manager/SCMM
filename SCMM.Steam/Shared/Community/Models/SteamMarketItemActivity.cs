using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;

namespace SCMM.Steam.Shared.Community.Models
{
    public class SteamMarketItemActivity
    {
        /// <summary>
        /// SellOrder, BuyOrder
        /// </summary>
        [JsonProperty("type")]
        public string PurchaseId { get; set; }

        public int Price { get; private set; }

        private string _priceText;
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

        private string _quantityText;
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
