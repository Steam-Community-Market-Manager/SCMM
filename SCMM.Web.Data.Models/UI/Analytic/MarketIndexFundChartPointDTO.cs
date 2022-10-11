namespace SCMM.Web.Data.Models.UI.Analytic
{
    public class MarketIndexFundChartPointDTO
    {
        public DateTime Date { get; set; }

        public long TotalSalesVolume { get; set; }

        public decimal TotalSalesValue { get; set; }

        public decimal AverageItemValue { get; set; }
    }
}
