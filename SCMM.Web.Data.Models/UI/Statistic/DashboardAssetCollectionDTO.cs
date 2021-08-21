namespace SCMM.Web.Data.Models.UI.Statistic
{
    public class DashboardAssetCollectionDTO
    {
        public ulong? CreatorId { get; set; }

        public string Name { get; set; }

        public string IconUrl { get; set; }

        public int Items { get; set; }

        public long? BuyNowPrice { get; set; }
    }
}
