using SCMM.Shared.Data.Models;
using SCMM.Steam.Data.Models.Enums;

namespace SCMM.Steam.Data.Models
{
    public class Price
    {
        public PriceType Type { get; set; }

        public IExchangeableCurrency Currency { get; set; }

        public long LowestPrice { get; set; }

        /// <summary>
        /// Zero == no supply. Null == unlimited supply. 
        /// </summary>
        public int? QuantityAvailable { get; set; } = 0;

        public bool IsAvailable { get; set; }

        public string Url { get; set; }
    }
}
