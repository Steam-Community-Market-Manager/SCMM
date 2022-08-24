namespace SCMM.Web.Data.Models.UI
{
    public interface IItemDescription
    {
        public ulong Id { get; set; }

        public ulong AppId { get; set; }

        public string Name { get; }

        public string ItemType { get; }

        public bool? HasGlow { get; set; }

        public string DominantColour { get; set; }

        public string BackgroundColour { get; }

        public string ForegroundColour { get; }

        public string IconUrl { get; }

        public DateTimeOffset? TimeCreated { get; set; }

        public DateTimeOffset? TimeAccepted { get; set; }
    }
}
