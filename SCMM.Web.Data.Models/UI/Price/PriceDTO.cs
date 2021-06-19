using SCMM.Steam.Data.Models.Enums;

namespace SCMM.Web.Data.Models.UI.Price
{
    public class PriceDTO
    {
        public PriceType Type { get; set; }

        public long BuyPrice { get; set; }

        public string BuyUrl { get; set; }
    }
}
