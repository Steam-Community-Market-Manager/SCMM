namespace SCMM.Web.Data.Models.UI.Statistic
{
    public class MarketTotalsStatisticDTO
    {
        public int Listings { get; set; }

        public long ListingsMarketValue { get; set; }

        public int? VolumeLast24hrs { get; set; }

        public long? VolumeMarketValueLast24hrs { get; set; }
    }
}
