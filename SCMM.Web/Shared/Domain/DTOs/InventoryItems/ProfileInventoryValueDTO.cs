using SCMM.Web.Shared.Domain.DTOs.Currencies;

namespace SCMM.Web.Shared.Domain.DTOs.InventoryItems
{
    public class ProfileInventoryValueDTO
    {
        public string SteamId { get; set; }

        public string Name { get; set; }

        public string AvatarUrl { get; set; }

        public string InventoryMosaicUrl { get; set; }

        public int Items { get; set; }

        public long? Invested { get; set; }

        public long MarketValue { get; set; }

        public long Market24hrMovement { get; set; }

        public long ResellValue { get; set; }

        public long ResellTax { get; set; }

        public long ResellProfit { get; set; }

        public CurrencyDTO Currency { get; set; }
    }
}
