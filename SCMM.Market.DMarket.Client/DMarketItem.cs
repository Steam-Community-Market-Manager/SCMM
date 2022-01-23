using System.Text.Json.Serialization;

namespace SCMM.Market.DMarket.Client
{
    public class DMarketItem
    {
        [JsonPropertyName("itemId")]
        public Guid ItemId { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("classId")]
        public string ClassId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("sell_min_price")]
        public string SellMinPrice { get; set; }

        [JsonPropertyName("sell_reference_price")]
        public string SellReference_Price { get; set; }

        [JsonPropertyName("sell_num")]
        public int SellNum { get; set; }
    }
}