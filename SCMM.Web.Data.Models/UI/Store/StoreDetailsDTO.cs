namespace SCMM.Web.Data.Models.UI.Store
{
    public class StoreDetailsDTO
    {
        public Guid Guid { get; set; }

        public string Id { get; set; }

        public string Name { get; set; }

        public DateTimeOffset? Start { get; set; }

        public DateTimeOffset? End { get; set; }

        public StoreItemDetailsDTO[] Items { get; set; }

        public string ItemsThumbnailUrl { get; set; }

        public string[] Media { get; set; }

        public string[] Notes { get; set; }

        public bool IsDraft { get; set; }
    }
}
