namespace SCMM.Web.Data.Models.UI.Analytic
{
    public class MarketIndexFundChartPointDTO
    {
        public DateTime Date { get; set; }

        public long Volume { get; set; }

        public long AdjustedVolume { get; set; }

        public decimal AverageValue { get; set; }

        public decimal CumulativeValue { get; set; }
    }
}
