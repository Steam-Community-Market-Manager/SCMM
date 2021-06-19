namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemDescriptionDTO : IItemDescription
    {
        public ulong Id { get; set; }

        public ulong AppId { get; set; }

        public string Name { get; set; }

        public string ItemType { get; set; }

        public string BackgroundColour { get; set; }

        public string ForegroundColour { get; set; }

        public string IconUrl { get; set; }
    }
}
