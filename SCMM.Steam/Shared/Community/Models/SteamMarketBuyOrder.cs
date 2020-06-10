using Newtonsoft.Json;

namespace SCMM.Steam.Shared.Community.Models
{
    public class SteamMarketBuyOrder
    {
        [JsonProperty("appid")]
        public string AppId { get; set; }

        [JsonProperty("hash_name")]
        public string HashName { get; set; }

        [JsonProperty("wallet_currency")]
        public string WalletCurrency { get; set; }

        [JsonProperty("price")]
        public string Price { get; set; }

        [JsonProperty("quantity")]
        public string Quantity { get; set; }

        [JsonProperty("quantity_remaining")]
        public string QuantityRemaining { get; set; }

        [JsonProperty("buy_orderid")]
        public string BuyOrderId { get; set; }

        [JsonProperty("description")]
        public SteamAssetDescription Description { get; set; }
    }
}
