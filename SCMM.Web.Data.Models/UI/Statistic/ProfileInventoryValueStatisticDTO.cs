namespace SCMM.Web.Data.Models.UI.Statistic
{
    public class ProfileInventoryValueStatisticDTO
    {
        public int Rank { get; set; }

        public string SteamId { get; set; }

        public string Name { get; set; }

        public string AvatarUrl { get; set; }

        public bool IsPrivate { get; set; }

        public bool IsBanned { get; set; }

        public bool IsBot { get; set; }

        public int Items { get; set; }

        public long Value { get; set; }

        public DateTimeOffset? LastUpdatedOn { get; set; }
    }
}
