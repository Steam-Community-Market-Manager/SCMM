using SCMM.Steam.Data.Models.Enums;
using System.Text.Json.Serialization;

namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemMarketPriceDTO
    {
        public MarketType MarketType { get; set; }

        public PriceFlags AcceptedPayments { get; set; }

        public long Price { get; set; }

        public long Fee { get; set; }

        [JsonIgnore]
        public long Total => (Price + Fee);

        public int? Supply { get; set; }

        public bool IsAvailable { get; set; }

        public string Url { get; set; }

        public override string ToString()
        {
            return $"{MarketType}:{Price}x{(Supply?.ToString() ?? "?")}";
        }
    }
}
