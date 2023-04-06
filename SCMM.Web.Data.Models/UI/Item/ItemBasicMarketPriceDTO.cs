using SCMM.Steam.Data.Models.Enums;

namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemBasicMarketPriceDTO
    {
        public MarketType MarketType { get; set; }

        public long Price { get; set; }

        public long Fee { get; set; }

        public int? Supply { get; set; }

        public bool IsAvailable { get; set; }
    }
}
