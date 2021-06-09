using SCMM.Shared.Data.Store;
using SCMM.Steam.Data.Models.Enums;

namespace SCMM.Steam.Data.Store
{
    public class Price : Entity
    {
        public PriceType Type { get; set; }

        public SteamCurrency Currency { get; set; }

        public long BuyPrice { get; set; }

        public string BuyUrl { get; set; }
    }
}
