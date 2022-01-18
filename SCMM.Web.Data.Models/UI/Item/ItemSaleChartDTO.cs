namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemSaleChartDTO
    {
        public DateTime Date { get; set; }

        public decimal Median { get; set; }

        public decimal High { get; set; }
        
        public decimal Low { get; set; }
        
        public decimal Open { get; set; }
        
        public decimal Close { get; set; }
        
        public long Volume { get; set; }
    }
}
