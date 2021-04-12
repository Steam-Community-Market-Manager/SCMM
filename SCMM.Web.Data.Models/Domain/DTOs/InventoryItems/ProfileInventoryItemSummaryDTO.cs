using SCMM.Web.Data.Models.Domain.DTOs.Currencies;
using SCMM.Web.Data.Models.Steam;
using SCMM.Web.Data.Models.UI;

namespace SCMM.Web.Data.Models.Domain.DTOs.InventoryItems
{
    public class ProfileInventoryItemSummaryDTO : IFilterableItem
    {
        public string SteamId { get; set; }

        public string SteamAppId { get; set; }

        public string Name { get; set; }

        public string BackgroundColour { get; set; }

        public string ForegroundColour { get; set; }

        public string IconUrl { get; set; }

        public CurrencyDTO Currency { get; set; }

        public int Quantity { get; set; }

        public long? Value { get; set; }

        public SteamProfileInventoryItemFlags Flags { get; set; }
    }
}
