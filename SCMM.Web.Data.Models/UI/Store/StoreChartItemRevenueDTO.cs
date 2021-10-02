namespace SCMM.Web.Data.Models.UI.Store
{
    public class StoreChartItemRevenueDTO
    {
        public string Name { get; set; }

        public decimal SalesTax { get; set; }

        public decimal AuthorRevenue { get; set; }

        public decimal PlatformRevenue { get; set; }

        public decimal PublisherRevenue { get; set; }

        public decimal Total => (SalesTax + AuthorRevenue + PlatformRevenue + PublisherRevenue);
    }
}
