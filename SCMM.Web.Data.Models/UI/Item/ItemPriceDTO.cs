using SCMM.Steam.Data.Models.Enums;

namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemPriceDTO
    {
        public PriceType Type { get; set; }

        public long LowestPrice { get; set; }

        public int? QuantityAvailable { get; set; }

        public bool IsAvailable { get; set; }

        public string Url { get; set; }
    }
}
