namespace SCMM.Web.Data.Models.UI.Statistic
{
    public class StoreTopSellerPositionChartPointDTO
    {
        public DateTime Timestamp { get; set; }

        public int Position { get; set; }

        public int Total { get; set; }

        public bool IsActive { get; set; }
    }
}
