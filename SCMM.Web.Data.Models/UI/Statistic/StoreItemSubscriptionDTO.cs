namespace SCMM.Web.Data.Models.UI.Statistic
{
    public class StoreItemSubscriptionDTO
    {
        public string Name { get; set; }

        public string IconAccentColour { get; set; }

        public string IconUrl { get; set; }

        public int Position { get; set; }

        public List<StoreItemSubscriberSnapshotDTO> SubscriberTimeline { get; set; }
    }
}
