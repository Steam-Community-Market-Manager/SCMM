using SCMM.Web.Shared.Data.Models.Steam;
using SCMM.Web.Shared.Data.Models.UI;
using SCMM.Web.Shared.Domain.DTOs.Currencies;

namespace SCMM.Web.Shared.Domain.DTOs.InventoryItems
{
    public class ProfileInventoryItemWishDTO : IFilterableItem
    {
        public string SteamId { get; set; }

        public string SteamAppId { get; set; }

        public string Name { get; set; }

        public string BackgroundColour { get; set; }

        public string ForegroundColour { get; set; }

        public string IconUrl { get; set; }

        public CurrencyDTO Currency { get; set; }

        public int Supply { get; set; }

        public int Demand { get; set; }

        public long? BuyAskingPrice { get; set; }

        public long? BuyNowPrice { get; set; }

        public long? Last24hrSales { get; set; }

        public SteamProfileMarketItemFlags Flags { get; set; }
    }
}
