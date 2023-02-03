namespace SCMM.Shared.Data.Models.Statistics
{
    public class MarketStatusStatistic
    {
        public int TotalItems { get; set; }

        public long TotalListings { get; set; }

        public DateTimeOffset? LastUpdatedItemsOn { get; set; }

        public DateTimeOffset? LastUpdateErrorOn { get; set; }

        public string LastUpdateError { get; set; }
    }
}
