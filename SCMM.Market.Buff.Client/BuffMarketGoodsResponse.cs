using System.Text.Json.Serialization;

namespace SCMM.Market.Buff.Client
{
    public class BuffMarketGoodsResponse
    {
        [JsonPropertyName("items")]
        public IEnumerable<BuffItem> Items { get; set; }

        [JsonPropertyName("page_num")]
        public int PageNum { get; set; }

        [JsonPropertyName("page_size")]
        public int PageSize { get; set; }

        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("total_page")]
        public int TotalPage { get; set; }
    }
}
