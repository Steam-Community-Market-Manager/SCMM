using SCMM.Steam.Data.Models.Enums;

namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemPriceDTO
    {
        public PriceType Type { get; set; }

        public long BuyPrice { get; set; }

        public string BuyUrl { get; set; }

        public bool IsAvailable { get; set; }
    }
}
