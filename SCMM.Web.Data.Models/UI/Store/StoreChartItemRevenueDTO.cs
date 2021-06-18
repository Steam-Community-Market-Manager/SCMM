
namespace SCMM.Web.Data.Models.UI.Store
{
    public class StoreChartItemRevenueDTO
    {
        public string Name { get; set; }

        public decimal AuthorRoyalties { get; set; }

        public decimal PlatformFees { get; set; }

        public decimal PublisherRevenue { get; set; }

        public decimal Total => (AuthorRoyalties + PlatformFees + PublisherRevenue);
    }
}
