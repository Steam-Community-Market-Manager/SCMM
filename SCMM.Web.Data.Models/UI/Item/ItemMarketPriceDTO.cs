using SCMM.Steam.Data.Models.Enums;
using System.Text.Json.Serialization;

namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemMarketPriceDTO
    {
        public PriceTypes Type { get; set; }

        public MarketType MarketType { get; set; }

        public long Price { get; set; }

        public long Fee { get; set; }

        [JsonIgnore]
        public long Total => (Price + Fee);

        public int? Supply { get; set; }

        public bool IsAvailable { get; set; }

        public string Url { get; set; }
    }
}
