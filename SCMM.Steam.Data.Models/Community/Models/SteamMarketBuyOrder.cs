using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamMarketBuyOrder
    {
        [JsonPropertyName("appid")]
        public ulong AppId { get; set; }

        [JsonPropertyName("hash_name")]
        public string HashName { get; set; }

        [JsonPropertyName("wallet_currency")]
        public string WalletCurrency { get; set; }

        [JsonPropertyName("price")]
        public string Price { get; set; }

        [JsonPropertyName("quantity")]
        public string Quantity { get; set; }

        [JsonPropertyName("quantity_remaining")]
        public string QuantityRemaining { get; set; }

        [JsonPropertyName("buy_orderid")]
        public string BuyOrderId { get; set; }

        [JsonPropertyName("description")]
        public SteamAssetClass Description { get; set; }
    }
}
