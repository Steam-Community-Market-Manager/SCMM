namespace SCMM.Web.Shared.Domain.DTOs.InventoryItems
{
    public class ProfileInventoryTotalsDTO
    {
        public string SteamId { get; set; }

        public int TotalItems { get; set; }

        public long TotalInvested { get; set; }

        public long TotalMarketValue { get; set; }

        public long TotalMarket24hrMovement { get; set; }

        public long TotalResellValue { get; set; }

        public long TotalResellProfit { get; set; }
    }
}
