using System;

namespace SCMM.Web.Data.Models.UI.Statistic
{
    public class MarketActivityChartStatisticDTO
    {
        public DateTime Date { get; set; }

        public int Sales { get; set; }

        public decimal Revenue { get; set; }
    }
}
