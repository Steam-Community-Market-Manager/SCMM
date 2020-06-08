using Newtonsoft.Json;
using SCMM.Steam.Shared.Models;

namespace SCMM.Steam.Shared.Responses.Community
{
    public class SteamMarketItemOrdersHistogramResponse : SteamResponse
    {
        [JsonProperty("price_prefix")]
        public string PricePrefix { get; set; }

        [JsonProperty("price_suffix")]
        public string PriceSuffix { get; set; }

        [JsonProperty("sell_order_table")]
        public SteamMarketItemOrder[] SellOrderTable { get; set; }

        [JsonProperty("buy_order_table")]
        public SteamMarketItemOrder[] BuyOrderTable { get; set; }
    }
}
