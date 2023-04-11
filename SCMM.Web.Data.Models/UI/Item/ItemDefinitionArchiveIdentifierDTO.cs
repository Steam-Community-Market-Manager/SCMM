namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemDefinitionArchiveIdentifierDTO
    {
        public string Id { get; set; }

        public string Digest { get; set; }

        public int Size { get; set; }

        public int ItemCount { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
    }
}
